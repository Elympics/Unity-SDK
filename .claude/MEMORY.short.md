# Shared agent memory (short)

Generated from `MEMORY.md`; don't edit by hand. Details, reasons, examples and time scopes: `MEMORY.md`.

## Code
- **jslib/jspre are ES2020 and call C# through makeDynCall:** no class fields or `||=` (`?.`/`??` are fine). Use `{{{ makeDynCall('sig', 'ptr') }}}(…)`, never `Module.dynCall_*`; no macros in `.jspre`. Test WebGL callbacks with WebAssembly.Table off and on.
- **Cross-assembly use needs an asmdef reference:** add the source assembly GUID to the consumer `.asmdef` `references` (`InternalsVisibleTo` isn't enough). Grep factory call sites for `var`-inferred uses.
- **Create a handshake's completion source before connecting; build WebRtcConfig from Default:** set up the TCS/subscription before the call that triggers the reply. Use `WebRtcConfig.Default` + field assignments, never a bare initializer.
- **UITK Bind() discards earlier .value writes:** don't set bound fields in `CreateInspectorGUI`/`PrepareInspectorTree`. Write the serialized property or use `OnValidate`.
- **Multiplayer Play Mode's CurrentPlayer throws without the package:** read MPPM state only through `PlayModeInstance`.
- **Vendored dependencies: Elympics names, own GUIDs, name references:** `Elympics.`-prefixed assemblies, fresh asset GUIDs (never upstream `.meta`), references by name; keep provenance files.

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
- **The running Editor caches EditorPrefs:** force Editor state with a temporary in-Editor code change, not external registry writes.
- **Test Runner window runs write TestResults.xml only through the performance package:** keep `com.unity.test-framework.performance` in the host, and check `start-time` before trusting the file.
- **WebGL linked assemblies: read the build, not ScriptingAssemblies.json:** use `ManagedStripped` right after the build, or the `.dll` names in the build's `global-metadata.dat`.
- **Domain reload off keeps NUnit ValueSource objects between runs:** don't mutate source objects; create them inside the test.
- **Unity's Mono HttpListener can't accept WebSockets:** test against a `TcpListener` echo server with a manual handshake and `WebSocket.CreateFromStream`.
- **Driving the Editor through the pipeline CLI:** global flags before `command`; `recompile` refuses while errors exist; `eval` has a ~5 s main-thread limit and sees public API only.

## Configuration
- **The active game is availableGames[0]; duplicate game IDs resolve to the first entry:** list order decides the gameplay scene when IDs repeat.

## Git
- **Pre-edit baseline via git show, never git stash; snapshot unstaged work before editing:** use `git show :path` / `HEAD:path`. Copy files with unstaged changes before editing.
