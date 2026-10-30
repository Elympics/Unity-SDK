#!/usr/bin/env bash

set -x

echo "Testing for $TEST_PLATFORM, Unit Type: $TESTING_TYPE"

RAW_TEST_RESULTS_PATH="$UNITY_DIR/$TEST_PLATFORM-results.xml"
JUNIT_XSLT_PATH="$CI_PROJECT_DIR/.scripts/ci/xslt/nunit3-junit.xslt"
TEST_RESULTS_PATH="$CI_PROJECT_DIR/$TEST_PLATFORM-results.xml"

COVERAGE_RESULTS_PATH="$CI_PROJECT_DIR/coverage"
PACKAGE_MANIFEST_PATH="$UNITY_DIR/Packages/manifest.json"
CODE_COVERAGE_PACKAGE="com.unity.testtools.codecoverage"

# CODE_COVERAGE=true instruments the run, which is slower and can keep objects alive, so garbage-collection tests need an uninstrumented run.
# COVERAGE_ASSEMBLY_FILTERS and COVERAGE_PATH_FILTERS: Code Coverage filters, one per line.
COVERAGE_ARGS=()
if [ "${CODE_COVERAGE:-false}" = "true" ]; then
mv "$PACKAGE_MANIFEST_PATH" "$PACKAGE_MANIFEST_PATH.old"
jq -r '.dependencies["'"$CODE_COVERAGE_PACKAGE"'"] = "1.3.0"' "$PACKAGE_MANIFEST_PATH.old" > "$PACKAGE_MANIFEST_PATH"

mkdir "$COVERAGE_RESULTS_PATH"

COVERAGE_ARGS=(
  -enableCodeCoverage
  -coverageResultsPath "$COVERAGE_RESULTS_PATH"
  -coverageOptions "generateAdditionalReports;assemblyFilters:$(paste -sd, <<< "$COVERAGE_ASSEMBLY_FILTERS");pathFilters:$(paste -sd, <<< "$COVERAGE_PATH_FILTERS")"
  -debugCodeOptimization
)
fi

rm -f "$RAW_TEST_RESULTS_PATH"

"$(dirname "${BASH_SOURCE[0]}")/unity_editor.sh" \
  -projectPath "$UNITY_DIR" \
  -runTests \
  -testPlatform "$TEST_PLATFORM" \
  -testResults "$RAW_TEST_RESULTS_PATH" \
  -logFile /dev/stdout \
  -batchmode \
  -nographics \
  "${COVERAGE_ARGS[@]}"

UNITY_EXIT_CODE=$?

# Unity can abort (134) or segfault (139) on quit (com.unity.webrtc threads) after it has saved the results; the results then decide the outcome.
if [ $UNITY_EXIT_CODE -eq 134 ] || [ $UNITY_EXIT_CODE -eq 139 ]; then
  TEST_RUN=$(grep -m1 -o '<test-run [^>]*' "$RAW_TEST_RESULTS_PATH" 2>/dev/null)
  TOTAL_COUNT=$(echo "$TEST_RUN" | grep -o ' total="[0-9]*"' | grep -o '[0-9][0-9]*')
  FAILED_COUNT=$(echo "$TEST_RUN" | grep -o ' failed="[0-9]*"' | grep -o '[0-9][0-9]*')
  RUN_RESULT=$(echo "$TEST_RUN" | grep -o ' result="[^"]*"' | cut -d'"' -f2)
  if [ -n "$TOTAL_COUNT" ] && [ "$TOTAL_COUNT" -gt 0 ] && [ -n "$FAILED_COUNT" ]; then
    echo "WARNING: Unity exited with code $UNITY_EXIT_CODE after saving the test results ($TOTAL_COUNT run, $FAILED_COUNT failed, result $RUN_RESULT); using the results instead"
    if [ "$FAILED_COUNT" -eq 0 ] && [[ "$RUN_RESULT" != Failed* ]]; then UNITY_EXIT_CODE=0; else UNITY_EXIT_CODE=2; fi
    if [ ${#COVERAGE_ARGS[@]} -gt 0 ] && [ -z "$(ls -A "$COVERAGE_RESULTS_PATH" 2>/dev/null)" ]; then
      echo "WARNING: the crash left no coverage report in $COVERAGE_RESULTS_PATH"
    fi
  fi
fi

function process_test_results {
  if [ "$TESTING_TYPE" == 'JUNIT' ]; then
    echo "Converting results to JUNit for analysis"
    xsltproc -o "$TEST_RESULTS_PATH" "$JUNIT_XSLT_PATH" "$RAW_TEST_RESULTS_PATH"
  else
    echo "Not converting results to JUNit"
    cp "$RAW_TEST_RESULTS_PATH" "$TEST_RESULTS_PATH"
  fi
}

function print_failed_tests {
  # Shows failed and inconclusive tests, and fixtures whose setup or teardown failed, in the job log;
  # otherwise they are only in the results artifact. Parents and children of a failure (site Child/Parent) would repeat it.
  awk '
    /<test-(case|suite) / && /result="(Failed|Inconclusive)"/ && !/site="(Child|Parent)"/ {
      match($0, /fullname="[^"]*"/); name = substr($0, RSTART + 10, RLENGTH - 11)
      match($0, /result="[^"]*"/); result = toupper(substr($0, RSTART + 8, RLENGTH - 9))
      print result ": " name ($0 ~ /<test-suite / ? " (fixture)" : ""); inside = 1; next
    }
    inside && (/<\/(failure|reason)>/ || /<test-(case|suite) /) { inside = 0; next }
    inside && !/^[[:space:]]*<\/?(properties|property|failure|reason)[ >\/]/ { print "    " $0 }
  ' "$RAW_TEST_RESULTS_PATH"
}

if [ $UNITY_EXIT_CODE -eq 0 ]; then
  echo "Run succeeded, no failures occurred"
  process_test_results
elif [ $UNITY_EXIT_CODE -eq 2 ]; then
  echo "Run succeeded, some tests failed"
  print_failed_tests
  process_test_results
elif [ $UNITY_EXIT_CODE -eq 3 ]; then
  echo "Run failure (other failure)"
else
  echo "Unexpected exit code $UNITY_EXIT_CODE"
fi

exit $UNITY_EXIT_CODE
