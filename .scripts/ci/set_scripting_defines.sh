#!/usr/bin/env bash
# Sets the testing project's scripting defines (';'-separated) for every target the Editor may compile with.
set -eo pipefail

echo "Setting scripting defines: $1"

PROJECT_SETTINGS_PATH=${UNITY_DIR}/ProjectSettings/ProjectSettings.asset
PROJECT_SETTINGS_HEADER=$(sed -e '/^---/q' "$PROJECT_SETTINGS_PATH")
PROJECT_SETTINGS_BODY=$(sed -e '1,/^---/d' "$PROJECT_SETTINGS_PATH" \
  | yq e '.PlayerSettings.scriptingDefineSymbols = {"Standalone": "'"$1"'", "Server": "'"$1"'", "WebGL": "'"$1"'"}' -)

printf '%s\n%s\n' "$PROJECT_SETTINGS_HEADER" "$PROJECT_SETTINGS_BODY" > "$PROJECT_SETTINGS_PATH"
