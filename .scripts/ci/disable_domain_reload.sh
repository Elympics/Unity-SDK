#!/usr/bin/env bash
# Enables Enter Play Mode Options with Domain Reload disabled in the testing project,
# as Unity 6.6+ defaults to for new projects and CoreCLR Unity requires.
set -e
SETTINGS="$UNITY_DIR/ProjectSettings/EditorSettings.asset"
sed -i 's/m_EnterPlayModeOptionsEnabled: .*/m_EnterPlayModeOptionsEnabled: 1/; s/m_EnterPlayModeOptions: .*/m_EnterPlayModeOptions: 1/' "$SETTINGS"
grep -q "m_EnterPlayModeOptionsEnabled: 1" "$SETTINGS"
grep -q "m_EnterPlayModeOptions: 1" "$SETTINGS"
