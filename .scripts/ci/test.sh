#!/usr/bin/env bash

set -x

echo "Testing for $TEST_PLATFORM, Unit Type: $TESTING_TYPE"

RAW_TEST_RESULTS_PATH="$UNITY_DIR/$TEST_PLATFORM-results.xml"
JUNIT_XSLT_PATH="$CI_PROJECT_DIR/.scripts/ci/xslt/nunit3-junit.xslt"
TEST_RESULTS_PATH="$CI_PROJECT_DIR/$TEST_PLATFORM-results.xml"

COVERAGE_RESULTS_PATH="$CI_PROJECT_DIR/coverage"
PACKAGE_MANIFEST_PATH="$UNITY_DIR/Packages/manifest.json"
CODE_COVERAGE_PACKAGE="com.unity.testtools.codecoverage"

# CODE_COVERAGE=false runs without coverage instrumentation, which can keep objects alive (see ElympicsEventTests.RaiseAfterGC).
COVERAGE_ARGS=()
if [ "${CODE_COVERAGE:-true}" != "false" ]; then
mv "$PACKAGE_MANIFEST_PATH" "$PACKAGE_MANIFEST_PATH.old"
jq -r '.dependencies["'"$CODE_COVERAGE_PACKAGE"'"] = "1.3.0"' "$PACKAGE_MANIFEST_PATH.old" > "$PACKAGE_MANIFEST_PATH"

mkdir "$COVERAGE_RESULTS_PATH"

COVERAGE_ASSEMBLY_FILTERS=(
  "+Elympics"
  "+Elympics.*"
  "+Elympics.Editor.*"
  "-Elympics.Tests"
  "-Elympics.Tests.*"
  "-Elympics.Editor.Tests"
  "-Elympics.Editor.Tests.*"
  "+AssemblyCommunicator"
  "+SmartContractService"
  "+SmartContractService.*"
  "-SmartContractService.Test"
  "-SmartContractService.Test.*"
)
COVERAGE_PATH_FILTERS=(
  "-**/Runtime/Plugins/**"
  "-**/GameEngine/Libraries/**"
)

COVERAGE_ASSEMBLY_FILTERS_JOINED=$(IFS=, ; echo "${COVERAGE_ASSEMBLY_FILTERS[*]}")
COVERAGE_PATH_FILTERS_JOINED=$(IFS=, ; echo "${COVERAGE_PATH_FILTERS[*]}")
COVERAGE_ARGS=(
  -enableCodeCoverage
  -coverageResultsPath "$COVERAGE_RESULTS_PATH"
  -coverageOptions "generateAdditionalReports;assemblyFilters:$COVERAGE_ASSEMBLY_FILTERS_JOINED;pathFilters:$COVERAGE_PATH_FILTERS_JOINED"
  -debugCodeOptimization
)
fi

${UNITY_EXECUTABLE:-xvfb-run --auto-servernum --server-args='-screen 0 640x480x24' unity-editor} \
  -projectPath "$UNITY_DIR" \
  -runTests \
  -testPlatform "$TEST_PLATFORM" \
  -testResults "$RAW_TEST_RESULTS_PATH" \
  -logFile /dev/stdout \
  -batchmode \
  -nographics \
  "${COVERAGE_ARGS[@]}"

UNITY_EXIT_CODE=$?

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
