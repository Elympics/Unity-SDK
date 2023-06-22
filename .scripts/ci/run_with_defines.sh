#!/usr/bin/env bash
set -e

echo "Running Testing Project with define directives $*"

PROJECT_SETTINGS_PATH=${UNITY_DIR}/ProjectSettings/ProjectSettings.asset
PROJECT_SETTINGS_HEADER=$(sed -e '/^---/q' "$PROJECT_SETTINGS_PATH")

cp "$PROJECT_SETTINGS_PATH" "${PROJECT_SETTINGS_PATH}.bak"

sed -i -e '1,/^---/d' "$PROJECT_SETTINGS_PATH"
yq e -i '.PlayerSettings.scriptingDefineSymbols = {}' "$PROJECT_SETTINGS_PATH"
for DEFINE_SYMBOL in "$@"
do
    yq e -i '.PlayerSettings.scriptingDefineSymbols += {"Standalone": "'"$DEFINE_SYMBOL"'"}' "$PROJECT_SETTINGS_PATH"
done

mv "$PROJECT_SETTINGS_PATH" "${PROJECT_SETTINGS_PATH}.tmp"
echo "$PROJECT_SETTINGS_HEADER" > "$PROJECT_SETTINGS_PATH"
cat "${PROJECT_SETTINGS_PATH}.tmp" >> "$PROJECT_SETTINGS_PATH"
rm -f "${PROJECT_SETTINGS_PATH}.tmp"

${UNITY_EXECUTABLE:-xvfb-run --auto-servernum --server-args='-screen 0 640x480x24' unity-editor} \
  -projectPath "$UNITY_DIR" \
  -logFile /dev/stdout \
  -batchmode \
  -nographics \
  -quit

UNITY_EXIT_CODE=$?

mv "${PROJECT_SETTINGS_PATH}.bak" "${PROJECT_SETTINGS_PATH}"

if [ $UNITY_EXIT_CODE -eq 0 ]; then
  echo "Run succeeded, no failures occurred"
else
  echo "Failed to run project"
  exit 1
fi
