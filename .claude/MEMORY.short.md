# Shared agent memory (short)

Generated from `MEMORY.md`; don't edit by hand. Details, reasons, examples and time scopes: `MEMORY.md`.

## Code
- **Handshake TCS before connecting; WebRtcConfig from Default:** never a bare `new WebRtcConfig { … }`.
- **UITK Bind() discards earlier .value writes:** write the serialized property or use `OnValidate`.
- **MPPM CurrentPlayer throws without the package:** read it only via `PlayModeInstance` (`ELYMPICS_MPPM`).
- **MPPM virtual players run the main Editor's scenes:** load the needed scene yourself.
- **Vendored deps:** `Elympics.`-prefixed assemblies, own GUIDs, references by name; see entry for BCL DLLs.
- **com.unity.webrtc threads in every Editor:** `ELYMPICS_LAZY_WEBRTC_CONTEXT`; `EditorWebRtcContext.EnsureInitialized()`.
- **WebGL size:** optional engine-module components go in their own never-referenced assembly.
- **com.unity.webrtc disposes all on quit in players:** tolerate `ObjectDisposedException` at shutdown.

## API and compatibility
- **com.unity.pipeline 0.8 is the floor:** keep it as the minimum; verify commands by listing, never by uploading.

## Verification
- **Compile and test evidence:** redirected `dotnet build`; tests via `unity command`, batchmode, or the open Editor.
- **Generated csprojs follow the build target:** also compile the other side of each platform `#if`.
- **Host Editor may be on the Server sub-target:** check `standaloneBuildSubtarget` before setting defines.
- **Editor API tests need EditorApiScope:** fakes for any code that may hit the backend; real transport deadlocks blocking tests.
- **Scratch Editors share the Elympics login:** give them a reachable endpoint; check the login afterwards.
- **Pipeline CLI:** flags after `command`; `eval` ~5 s, public API only; long work via `run_script`; always `UNITY_PROJECT_PATH`; tests total > 0.

## Configuration
- **Active game is availableGames[0]:** duplicate game IDs resolve to the first entry.

## Git
- **Baseline via git show, never git stash:** copy files with unstaged changes before editing.
- **New CI scripts need git's executable bit:** `git update-index --chmod=+x`.

## Situational (see MEMORY.md)
- StreamingAssets upload contract
- runInBackground = PlayerSetting
- BuildPlayer Server subtarget
- 6.5+ re-reports scripts
- Project Auditor skips asms
- Editor caches EditorPrefs
- Test Runner TestResults.xml
- WebGL linked assemblies
- Domain reload + ValueSource
- HttpListener no WebSockets
- New Editor rewrites sources
