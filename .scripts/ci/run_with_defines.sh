#!/usr/bin/env bash
set -e

echo "Running Testing Project with define directives $1"

PROJECT_SETTINGS_PATH=${UNITY_DIR}/ProjectSettings/ProjectSettings.asset

cp "$PROJECT_SETTINGS_PATH" "${PROJECT_SETTINGS_PATH}.bak"

"$(dirname "${BASH_SOURCE[0]}")/set_scripting_defines.sh" "$1"

UNITY_EXIT_CODE=0
"$(dirname "${BASH_SOURCE[0]}")/unity_editor.sh" \
  -projectPath "$UNITY_DIR" \
  -logFile /dev/stdout \
  -batchmode \
  -nographics \
  -quit || UNITY_EXIT_CODE=$?

mv "${PROJECT_SETTINGS_PATH}.bak" "${PROJECT_SETTINGS_PATH}"

if [ $UNITY_EXIT_CODE -eq 0 ]; then
  echo "Run succeeded, no failures occurred"
else
  echo "Failed to run project"
  exit "$UNITY_EXIT_CODE"
fi
