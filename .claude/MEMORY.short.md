# Shared agent memory (short)

Generated from `MEMORY.md`; don't edit by hand. Details, reasons, examples and time scopes: `MEMORY.md`.

## Code
- **jslib/jspre are ES2020 and call C# through makeDynCall:** no class fields or `||=` (`?.`/`??` are fine). Use `{{{ makeDynCall('sig', 'ptr') }}}(…)`, never `Module.dynCall_*`; no macros in `.jspre`. Test WebGL callbacks with WebAssembly.Table off and on.
- **Cross-assembly use needs an asmdef reference:** add the source assembly GUID to the consumer `.asmdef` `references` (`InternalsVisibleTo` isn't enough). Grep factory call sites for `var`-inferred uses.
- **Create a handshake's completion source before connecting; build WebRtcConfig from Default:** set up the TCS/subscription before the call that triggers the reply. Use `WebRtcConfig.Default` + field assignments, never a bare initializer.
- **UITK Bind() discards earlier .value writes:** don't set bound fields in `CreateInspectorGUI`/`PrepareInspectorTree`. Write the serialized property or use `OnValidate`.
- **Multiplayer Play Mode's CurrentPlayer throws without the package:** read MPPM state only through `PlayModeInstance`.
- **Vendored dependencies: Elympics names, own GUIDs, name references:** `Elympics.`-prefixed assemblies, fresh asset GUIDs (never upstream `.meta`), references by name; keep provenance files. Vendored BCL DLLs get `!UNITY_6000_5_OR_NEWER` (the Editor ships them from 6.5); DLLs referencing them need a retargeted `Unity6000.5/` copy.
- **com.unity.webrtc runs native threads in every Editor; ELYMPICS_LAZY_WEBRTC_CONTEXT stops them:** define it in Editor-only projects (CI); call `EditorWebRtcContext.EnsureInitialized()` before creating `RTCPeerConnection` in the Editor. A SIGSEGV in `libwebrtc.so` or a heap error on quit is this issue.
- **WebGL size: unused components in a linked assembly keep their engine modules:** put optional module-using components in their own assembly that core runtime code never references, with no load-time hooks; reach them via interfaces; don't enable vendored features that add components. Measure in a fresh project.

## Backend contracts
- **StreamingAssets upload contract:** versions are write-once. The Content-Type map (json/xml/octet-stream) is a verified allowlist. Keep the bundles → catalogs → `variants.meta.json` order. Don't validate baked catalog URLs.

## API and compatibility
- **Shipped [PublicAPI]: add a sibling (inactive until 1.0.0):** from 1.0.0, add a sibling method instead of refactoring a `[PublicAPI]` one.
- **Breaking changes are allowed before 1.0.0; ask before designing for compatibility:** no `[Obsolete]` cycles or migrations, but they must increase the minor version. Document in release notes. Ask before engineering compatibility.
- **com.unity.pipeline: reference both Unity.Pipeline and Unity.Pipeline.Attributes:** supports pipeline 0.5+; `#if` on versionDefines where the API differs; verify commands by listing, never by running uploads.

## Verification
- **dotnet format is not a compile check:** never cite its exit code as proof that code compiles.
- **Compile and test evidence:** compile with the redirected `dotnet build`. Test with batchmode if a seat is free, `unity command` if the host has `com.unity.pipeline`, or else the open Editor plus `ScriptAssemblies` mtimes, `Editor.log` offsets and `TestResults.xml`.
- **Generated csprojs follow the Editor's build target:** check `<DefineConstants>` for a platform (e.g. `UNITY_WEBGL`) and also compile the other side of each `#if`: build refs normally, then rebuild the Elympics projects with escaped `DefineConstants` and `BuildProjectReferences=false`. Pass platform capabilities into code under test.
- **The host Editor may be on the Dedicated Server sub-target:** check `standaloneBuildSubtarget`; with `Server`, set defines on `NamedBuildTarget.Server` and confirm with `CompilationPipeline.GetAssemblies(...).defines`.
- **Project Auditor skips assemblies it can't compile; NSubstitute breaks the ValueTask ones:** audit without the Tests folders (NSubstitute); prove coverage with a canary and grep the log for `error CS`.
- **The running Editor caches EditorPrefs:** force Editor state with a temporary in-Editor code change, not external registry writes.
- **Test Runner window runs write TestResults.xml only through the performance package:** keep `com.unity.test-framework.performance` in the host, and check `start-time` before trusting the file.
- **WebGL linked assemblies: read the build, not ScriptingAssemblies.json:** use `ManagedStripped` right after the build, or the `.dll` names in the build's `global-metadata.dat`.
- **Domain reload off keeps NUnit ValueSource objects between runs:** don't mutate source objects; create them inside the test.
- **Unity's Mono HttpListener can't accept WebSockets:** test against a `TcpListener` echo server with a manual handshake and `WebSocket.CreateFromStream`.
- **Driving the Editor through the pipeline CLI:** global flags before `command`; `recompile` refuses while errors exist; `eval` has a ~5 s main-thread limit and sees public API only; synchronous commands stop after 30 s, so schedule long work via `run_script` + `EditorApplication.delayCall`; `switch_build_target` before `build --target`; array args as JSON.
- **A newer Unity Editor rewrites package sources:** after an Editor upgrade, review `git status` in both packages (Script Updater edits are often incomplete).

## Configuration
- **The active game is availableGames[0]; duplicate game IDs resolve to the first entry:** list order decides the gameplay scene when IDs repeat.

## Git
- **Pre-edit baseline via git show, never git stash; snapshot unstaged work before editing:** use `git show :path` / `HEAD:path`. Copy files with unstaged changes before editing.
- **New CI scripts need git's executable bit:** `git update-index --chmod=+x`; check `git ls-files -s` shows `100755`.
