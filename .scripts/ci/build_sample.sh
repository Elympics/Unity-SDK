#!/usr/bin/env bash
# Builds a sample of the testing project for $BUILD_TARGET (StandaloneLinux64 or WebGL) with CiBuild.cs.
# Expects UNITY_DIR (the testing project) and CI_BUILD_SCENES (';'-separated scene paths relative to UNITY_DIR).
set -e

# Unity 6.6+ needs 1 GiB of shared memory at startup (Unity Data Store).
df -h /dev/shm || true

mkdir -p "$UNITY_DIR/Assets/Editor"
cp "$(dirname "${BASH_SOURCE[0]}")/CiBuild.cs" "$UNITY_DIR/Assets/Editor/CiBuild.cs"

export CI_BUILD_OUTPUT="${CI_BUILD_OUTPUT:-$UNITY_DIR/Build}"

"$(dirname "${BASH_SOURCE[0]}")/unity_editor.sh" \
  -projectPath "$UNITY_DIR" \
  -buildTarget "$BUILD_TARGET" \
  -executeMethod CiBuild.Build \
  -quit \
  -logFile /dev/stdout \
  -batchmode \
  -nographics
