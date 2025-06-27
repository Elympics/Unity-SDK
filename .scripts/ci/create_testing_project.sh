#!/usr/bin/env bash
set -e

echo "Creating Testing Project"
PACKAGE_FOLDER=${PACKAGE_DIR}/

${UNITY_EXECUTABLE:-xvfb-run --auto-servernum --server-args='-screen 0 640x480x24' unity-editor} \
  -createProject "$UNITY_DIR" \
  -logFile /dev/stdout \
  -batchmode \
  -nographics \
  -quit

UNITY_EXIT_CODE=$?

if [ $UNITY_EXIT_CODE -eq 0 ]; then
  echo "Run succeeded, no failures occurred"
else
  echo "Failed to create project"
  exit 1
fi

echo "Moving Package files to appropriate directories"

mkdir -p "$PACKAGE_FOLDER"
cp -r Editor{,.meta} "$PACKAGE_FOLDER"
cp -r Runtime{,.meta} "$PACKAGE_FOLDER"
cp -r Tests{,.meta} "$PACKAGE_FOLDER"
cp package.json{,.meta} "$PACKAGE_FOLDER"
cp -r Samples~ "$PACKAGE_FOLDER"
mv "${PACKAGE_FOLDER}Samples~" "${PACKAGE_FOLDER}Samples"

echo "Elympics moved ✅"
