#!/usr/bin/env bash
set -e
set -x

echo "Generating solution files for created project"

"$(dirname "${BASH_SOURCE[0]}")/unity_editor.sh" \
  -projectPath $UNITY_DIR \
  -quit \
  -batchmode \
  -nographics \
  -executeMethod Packages.Rider.Editor.RiderScriptEditor.SyncSolution \
  -logFile /dev/stdout

UNITY_EXIT_CODE=$?

if [ $UNITY_EXIT_CODE -eq 0 ]; then
  echo "Run succeeded, no failures occurred";
elif [ $UNITY_EXIT_CODE -eq 2 ]; then
  echo "Run succeeded, some tests failed";
elif [ $UNITY_EXIT_CODE -eq 3 ]; then
  echo "Run failure (other failure)";
else
  echo "Unexpected exit code $UNITY_EXIT_CODE";
fi
