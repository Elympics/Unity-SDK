#!/usr/bin/env bash
# Creates the testing project in UNITY_DIR with the package embedded in PACKAGE_DIR.
# ProjectSettings/ and Library/ come from the runner cache; without them, two Unity runs create the project and import it.
set -e

run_unity() {
  "$(dirname "${BASH_SOURCE[0]}")/unity_editor.sh" \
    "$@" \
    -logFile /dev/stdout \
    -batchmode \
    -nographics \
    -quit
}

echo "Creating Testing Project"
PACKAGE_FOLDER=${PACKAGE_DIR}/
ASSETS_FOLDER=${UNITY_DIR}/Assets
PACKAGES_FOLDER=${UNITY_DIR}/Packages

NEW_PROJECT=
if [ -f "${UNITY_DIR}/ProjectSettings/ProjectSettings.asset" ]; then
  echo "ProjectSettings restored from the cache"
else
  # -createProject adds fewer default packages than opening a folder without ProjectSettings/.
  echo "Creating ProjectSettings"
  rm -rf "$UNITY_DIR"
  run_unity -createProject "$UNITY_DIR" || { UNITY_EXIT_CODE=$?; echo "Failed to create project"; exit "$UNITY_EXIT_CODE"; }
  NEW_PROJECT=1
fi

mkdir -p "$ASSETS_FOLDER" "$PACKAGES_FOLDER"
# Rider: generate_solution_files.sh calls its SyncSolution
echo '{ "dependencies": { "com.unity.ide.rider": "3.0.39" } }' > "${PACKAGES_FOLDER}/manifest.json"

echo "Moving Package files to appropriate directories"

mkdir -p "$PACKAGE_FOLDER"
cp -r Editor{,.meta} "$PACKAGE_FOLDER"
cp -r Runtime{,.meta} "$PACKAGE_FOLDER"
cp -r Tests{,.meta} "$PACKAGE_FOLDER"
cp package.json{,.meta} "$PACKAGE_FOLDER"
cp -r Samples~ "$PACKAGE_FOLDER"
mv "${PACKAGE_FOLDER}Samples~" "${ASSETS_FOLDER}/Samples"

echo "Elympics moved ✅"

if [ -n "$NEW_PROJECT" ]; then
  echo "Importing the project"
  run_unity -projectPath "$UNITY_DIR" || { UNITY_EXIT_CODE=$?; echo "Failed to import project"; exit "$UNITY_EXIT_CODE"; }
fi

# Keeps com.unity.webrtc's native context stopped in the Editor until a test needs it; it can crash Unity during reloads.
"$(dirname "${BASH_SOURCE[0]}")/set_scripting_defines.sh" "ELYMPICS_LAZY_WEBRTC_CONTEXT"
