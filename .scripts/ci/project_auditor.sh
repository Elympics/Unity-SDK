#!/usr/bin/env bash
# Runs Project Auditor on the testing project and compares this package's domain-reload and upgrade findings with
# .scripts/ci/project-auditor-baseline.txt (see ProjectAuditorCi.cs). Expects UNITY_DIR and PACKAGE_DIR.
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MANIFEST="$UNITY_DIR/Packages/manifest.json"

jq '.dependencies["com.unity.project-auditor"] = "3.1.1"' "$MANIFEST" > "$MANIFEST.tmp" && mv "$MANIFEST.tmp" "$MANIFEST"
mkdir -p "$UNITY_DIR/Assets/Editor"
cp "$SCRIPT_DIR/ProjectAuditorCi.cs" "$UNITY_DIR/Assets/Editor/ProjectAuditorCi.cs"

export CI_AUDIT_PACKAGE_PATH="Packages/$(basename "$PACKAGE_DIR")/"
export CI_AUDIT_BASELINE="$SCRIPT_DIR/project-auditor-baseline.txt"
export CI_AUDIT_OUTPUT="$CI_PROJECT_DIR/project-auditor-findings.txt"

"$(dirname "${BASH_SOURCE[0]}")/unity_editor.sh" \
  -projectPath "$UNITY_DIR" \
  -executeMethod ProjectAuditorCi.Run \
  -quit \
  -logFile /dev/stdout \
  -batchmode \
  -nographics
