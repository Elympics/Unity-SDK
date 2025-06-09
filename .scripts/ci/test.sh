#!/usr/bin/env bash

set -x

echo "Testing for $TEST_PLATFORM, Unit Type: $TESTING_TYPE"

RAW_TEST_RESULTS_PATH="$UNITY_DIR/$TEST_PLATFORM-results.xml"
JUNIT_XSLT_PATH="$CI_PROJECT_DIR/.scripts/ci/xslt/nunit3-junit.xslt"
TEST_RESULTS_PATH="$CI_PROJECT_DIR/$TEST_PLATFORM-results.xml"

COVERAGE_RESULTS_PATH="$CI_PROJECT_DIR/coverage"
PACKAGE_MANIFEST_PATH="$UNITY_DIR/Packages/manifest.json"
CODE_COVERAGE_PACKAGE="com.unity.testtools.codecoverage"

mv "$PACKAGE_MANIFEST_PATH" "$PACKAGE_MANIFEST_PATH.old"
jq -r '.dependencies["'"$CODE_COVERAGE_PACKAGE"'"] = "1.2.4"' "$PACKAGE_MANIFEST_PATH.old" > "$PACKAGE_MANIFEST_PATH"

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
${UNITY_EXECUTABLE:-xvfb-run --auto-servernum --server-args='-screen 0 640x480x24' unity-editor} \
  -projectPath "$UNITY_DIR" \
  -runTests \
  -testPlatform "$TEST_PLATFORM" \
  -testResults "$RAW_TEST_RESULTS_PATH" \
  -logFile /dev/stdout \
  -batchmode \
  -nographics \
  -enableCodeCoverage \
  -coverageResultsPath "$COVERAGE_RESULTS_PATH" \
  -coverageOptions "generateAdditionalReports;assemblyFilters:$COVERAGE_ASSEMBLY_FILTERS_JOINED;pathFilters:$COVERAGE_PATH_FILTERS_JOINED" \
  -debugCodeOptimization

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

if [ $UNITY_EXIT_CODE -eq 0 ]; then
  echo "Run succeeded, no failures occurred"
  process_test_results
elif [ $UNITY_EXIT_CODE -eq 2 ]; then
  echo "Run succeeded, some tests failed"
  process_test_results
elif [ $UNITY_EXIT_CODE -eq 3 ]; then
  echo "Run failure (other failure)"
else
  echo "Unexpected exit code $UNITY_EXIT_CODE"
fi

exit $UNITY_EXIT_CODE
