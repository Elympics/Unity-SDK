# Shared agent memory

Important, non-obvious knowledge about this package. Use this file as your memory for this package instead of your local/auto memory.

- Add an entry when you learn something important that the code, git history or CLAUDE.md doesn't already say. Update or remove an existing entry instead of duplicating it.
- Every entry has **Rule**, **Reason**, **Example** and **Time scope** (the condition under which to rewrite or remove it). Remove or rewrite entries whose time scope has passed.
- No personal or machine-specific data (user names, home-dir paths, local tool locations).
- Entries are grouped under `## Category` headings, each entry a `### Title`. Put a new entry under the category that fits, or add a new category if none does.
- After every edit, regenerate `MEMORY.short.md`: the same category headings, one bullet per entry with its title and a condensed Rule, no Reason/Example/Time scope. CLAUDE.md imports that file.

## Code

### jslib/jspre are ES2020 and call C# through makeDynCall
- **Rule:** no syntax newer than ES2020 in `.jslib`/`.jspre` files (no class fields, no `||=`; `?.` and `??` are fine). Call C# callbacks with `{{{ makeDynCall('vii', 'callback') }}}(a, b)`, never `Module.dynCall_*`. Don't put Emscripten macros in `.jspre` files.
- **Reason:** Unity 6.3–6.4 ship Emscripten 3.1.39 (acorn and Closure at ES2020; Unity 6.5+ ship 4.0.19). `tsc` doesn't reject newer syntax, so `check_js.sh` also parses with acorn. `makeDynCall` works with "Use WebAssembly.Table" on and off, and Emscripten adds `getWasmTableEntry` automatically; `.jspre` files are inserted verbatim. With the Table off, a `dynCall_<sig>` that isn't exported turns the call into a silent no-op (only assertion builds throw), so WebGL callback changes need a run with the Table off and on.
- **Example:** a canary `a ||= 1` passed `tsc` and failed the acorn step. The removed `dynamicCall.jspre` polyfilled `Module.dynCall_*` with `getWasmTableEntry`.
- **Time scope:** If the minimum Unity version reaches 6.5 (Emscripten 4.x), raise the ES level and the acorn `ecmaVersion`.

### Cross-assembly use needs an asmdef reference
- **Rule:** when a file starts using another assembly's types, add the source assembly (usually by GUID) to the consuming `.asmdef` `references`. `InternalsVisibleTo` alone doesn't make a type resolvable. When sweeping for affected files, also grep factory/constructor call sites, because `var`-inferred types never appear as text.
- **Reason:** a present `InternalsVisibleTo` looks like enough on inspection, and a type used only through `var` is missed by a type-name grep. Both misses only surface as a Unity compile error.
- **Example:** `WebGL/WebRtcClient.cs` (`Elympics.WebRtc.WebGl`) started using `IDataChannel` and needed `Elympics.MatchConnection` (`86d779950baf55b4783b8ef7d9337d73`) in its references. `var webRtcClient = WebRtcFactory.CreateClient(WebRtcConfig.Default);` in `Tests/Runtime/UnityConnectors/HalfRemote/HalfRemoteGameEngineServerTests.cs` was missed by a type-name grep. Other GUIDs: `Elympics.WebRtc.Common` `e2a0cb92652b07d409c674525eba884a`, `Elympics.WebRtc.Other` `598d50f0b99ecc342bb6bff69c950dbd`, `Elympics.WebRtc.WebGl` `884c905e7a86c7444ad05287e5b8b669`, `Elympics.WebRtc` `859bd52dcb739c84bb2b971eecc6b4e4`. `Tests/Runtime/Elympics.Tests.asmdef` uses assembly names instead of GUIDs.
- **Time scope:** If the asmdef layout changes (assemblies merged or split, GUIDs changed), update or remove this.

### Create a handshake's completion source before connecting; build WebRtcConfig from Default
- **Rule:** a completion source or subscription that catches an async server reply must exist before the call that can trigger that reply. Build `WebRtcConfig` as `var c = WebRtcConfig.Default; c.X = …;`, never as a bare `new WebRtcConfig { … }`.
- **Reason:** `GameServerClient` completes the handshake with `_sessionConnectedTcs?.TrySetResult(message)`, so a `ConnectedMessage` that arrives before the TCS exists is dropped silently. The client then waits out `SessionConnectTimeout`, and this only shows up against a real server. A bare initializer leaves `IceServers` null, so `Other/WebRtcClient` throws `ArgumentNullException`. A refactor can revive a code path that never actually ran, so don't assume that path is correct just because it predates your change.
- **Example:** splitting "connect the transport" from "wait for Connected" left TCS creation in the second step, and the server's immediate reply raced it.
- **Time scope:** If `GameServerClient` stops waiting for `ConnectedMessage` through a completion source, drop the first part. If `WebRtcConfig` gets non-null field defaults, drop the second.

### UITK Bind() discards earlier .value writes
- **Rule:** don't set a bound field's `.value` during `CreateInspectorGUI`/`PrepareInspectorTree`. Write the serialized property instead, or derive the value in `ElympicsGameConfig.OnValidate` or a computed property.
- **Reason:** binding runs after tree construction (`ManageGamesInElympicsWindow` calls `inspector.Bind(...)`, and Unity's inspector path does the same) and overwrites the write with no error. Writing after the bind instead dirties the asset just because a window was opened. Unbound fields set with `SetValueWithoutNotify` aren't affected.
- **Example:** re-deriving `scenePath` next to the initial `Update*()` calls in `ElympicsGameConfigEditor` compiled, looked right, and did nothing.
- **Time scope:** If `ElympicsGameConfigEditor` stops using UI Toolkit binding, remove this.

### Multiplayer Play Mode's CurrentPlayer throws without the package
- **Rule:** read Multiplayer Play Mode state only through `PlayModeInstance` (`IsVirtualPlayer`, `Tags`, `PlayerName`), never `CurrentPlayer` directly.
- **Reason:** in 6.3, `Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor` and `Tags` throw a NullReferenceException (from `SystemDataStore.GetMain`) when `com.unity.multiplayer.playmode` isn't installed, which is most games. `PlayModeInstance` treats that as the main Editor and caches the process-constant answer, which also keeps inspector repaints cheap.
- **Example:** `UsageStatisticsSender`'s `[InitializeOnLoad]` static constructor threw on Editor reload in a host without the package.
- **Time scope:** If Unity stops throwing there, or Elympics starts depending on the package, simplify `PlayModeInstance`.

### Vendored dependencies: Elympics names, own GUIDs, name references
- **Rule:** a vendored source dependency gets `Elympics.`-prefixed assembly names and fresh asset GUIDs (never the upstream `.meta` files). Elympics and PlayPad asmdefs reference it by name, not GUID. When re-vendoring, regenerate the GUIDs again and keep the `.<name>.repo`/`.sha1` provenance files.
- **Reason:** with upstream GUIDs, a game that also installs the official package can't import it ("GUID … conflicts … immutable folder"). Unity rejects duplicate asmdef names even when one is excluded by `defineConstraints`, so a "switch off our copy" approach is impossible. After an asmdef's GUID changes, Unity's compilation pipeline keeps resolving the old GUID (it survives refresh, a successful compile and a forced reimport), so GUID references to the new GUID fail in projects that upgrade.
- **Example:** UniTask 2.5.10 is `Elympics.UniTask*` in `Elympics.Cysharp.*`; MessagePack is `Elympics.MessagePack*` with its namespace unchanged, so `mpc` output is identical. Vendored BCL DLLs are different: from Unity 6000.5 the Editor references its own `BCLExtensions` (Unsafe, System.Text.Json, System.Collections.Immutable, …) in every script assembly, so a vendored copy needs the plugin define constraint `!UNITY_6000_5_OR_NEWER` (CS0433 otherwise). Precompiled DLLs that reference the renamed copy (`Elympics.Google.Protobuf`, `Elympics.Microsoft.NET.StringTools`) then fail in the player linker (compiles pass), so each has a `Unity6000.5/` copy whose reference was rewritten with Mono.Cecil to `System.Runtime.CompilerServices.Unsafe` 6.0.0.0; when re-vendoring either DLL, regenerate that copy.
- **Time scope:** If Unity package management can share or isolate dependencies between packages, revisit.

### com.unity.webrtc runs native threads in every Editor; ELYMPICS_LAZY_WEBRTC_CONTEXT stops them
- **Rule:** in Unity projects that run Editor-only work (CI testing projects, measurement projects), define `ELYMPICS_LAZY_WEBRTC_CONTEXT` so the WebRTC context starts only when Elympics creates a peer connection. Code that creates `RTCPeerConnection` in the Editor must call `EditorWebRtcContext.EnsureInitialized()` first. Treat a SIGSEGV with `libwebrtc.so` frames, or a glibc heap error on quit, as this issue.
- **Reason:** com.unity.webrtc 3.0.0 (the latest; no fix available) starts its native context after every assembly reload (`ContextManager`, `[InitializeOnLoad]`), even when nothing uses WebRTC. Its threads crashed Unity during `ReloadAssembly` (exit code 255) and probably on quit. The define is opt-in, because a game that uses com.unity.webrtc itself in the Editor needs the context running. `EditorWebRtcContext` uses webrtc internals (`s_context`, `InitializeInternal`, `DisposeInternal`, the `ContextManager` handlers); `TestEditorWebRtcContext` fails when they disappear.
- **Example:** a PlayPad EditMode job on 6.6 crashed right after compiling, before any test ran (`Native stacktrace: … libwebrtc.so … Begin MonoManager ReloadAssembly`). With the define, the host Editor showed `s_context == null` after reloads, and the half-remote `WebRtcTest` still connected.
- **Time scope:** If com.unity.webrtc fixes its Editor lifecycle, or Elympics stops depending on it, remove the define and this entry.

### WebGL size: unused components in a linked assembly keep their engine modules
- **Rule:** keep optional components that use an engine module (2D physics, Animation, uGUI…) in their own assembly that runtime code of the always-linked assemblies never references, and give that assembly no load-time hooks (`[RuntimeInitializeOnLoadMethod]`, static self-registration, `[Preserve]`). Core code reaches them only through interfaces they implement (e.g. `IReplayPhysicsStep`). Don't enable vendored-library features that add components (UniTask's uGUI/physics triggers). Measure size changes with a fresh project's WebGL build, not the host.
- **Reason:** IL2CPP stripping keeps MonoBehaviour-derived types of a linked assembly even when no scene uses them, and each keeps its engine module and the module's dependencies. The host project can't show a minimal game's size: its config pulls in the samples, which link uGUI.
- **Example:** UniTask's triggers kept uGUI, UI Toolkit, TextCore, IMGUI, Tilemap and 2D physics in every Elympics WebGL build (a minimal game went from 29.8 to 20.2 MB raw when they were disabled). Moving the 2D and Animator synchronizers to `Elympics.Physics2D`/`Elympics.Animation` took it to 18.5 MB; a scene with the 2D synchronizer links `Elympics.Physics2D` and the 2D module again. An empty 6.3 project is 13.9 MB.
- **Time scope:** If Unity's linker stops preserving unused MonoBehaviours, or CoreCLR players change stripping, re-measure and update.

## Backend contracts

### StreamingAssets upload contract
- **Rule:** treat the per-extension Content-Type map as a verified allowlist: `.json` → `application/json`, `.xml` → `application/xml`, everything else `application/octet-stream`. Don't add an extension without a real upload confirming it. Keep the upload order bundles → catalogs → `variants.meta.json`. Don't validate catalog URLs baked into builds.
- **Reason:** a version is write-once (re-initializing returns 400, and there's no resume), so any failed PUT uses up that version number. The upload order makes a partial version fail closed. The signed URL covers `content-type`, so a mismatch surfaces as GCS `SignatureDoesNotMatch`, not as a content-type error. The game runner overwrites the baked URLs at runtime. Bucket layout: `{bucket}/{gameId}/sa/{version}/{variant}/aa/…`.
- **Example:** sending `text/plain` for `.hash` failed with `SignatureDoesNotMatch` and used up that version number.
- **Time scope:** If the backend `/client-builds/streaming-assets` contract changes (resumable uploads, `Content-Type` no longer signed), rewrite this.

## API and compatibility

### Shipped [PublicAPI]: add a sibling (inactive until 1.0.0)
- **Rule:** from 1.0.0 on, don't refactor or change a `[PublicAPI]` method to share code with a new one. Add a sibling method, accept the duplication, and prove the original is untouched (`git diff --stat` shows insertions only). Below 1.0.0, refactoring is allowed.
- **Reason:** these methods are the documented surface games call, for example via `-executeMethod`, and nothing in this repo tests that path.
- **Example:** a new entry point that needs most of `ElympicsWebIntegration.UploadStreamingAssetsInBatchmode` gets its own method next to it.
- **Time scope:** Inactive below 1.0.0. At 1.0.0 or above, remove the "inactive" note.

### Breaking changes are allowed before 1.0.0; ask before designing for compatibility
- **Rule:** removing or renaming public API or serialized members doesn't need an `[Obsolete]` cycle or a migration. It must increase the minor version number, never only the patch (for example `0.27.x` → `0.28.0`). Document the change in the release notes. When a compatibility hazard comes up, ask whether compatibility is required before engineering for it.
- **Reason:** the SDK is pre-1.0, and releases on this track are breaking anyway. Below 1.0.0, the minor number is what signals a breaking change to consumers, so a breaking patch release would reach games that only expect fixes.
- **Example:** `Runtime/Communication/Respect` was deleted without deprecation. Removing a serialized enum member renumbers the values in existing `ElympicsGameConfig.asset` files. The decision was a release-note reminder, not pinned enum values plus an `OnValidate` migration.
- **Time scope:** If the version is 1.0.0 or above, remove this.

### com.unity.pipeline: reference both Unity.Pipeline and Unity.Pipeline.Attributes
- **Rule:** assemblies that declare `[CliCommand]`/`[CliArg]` reference both `Unity.Pipeline` and `Unity.Pipeline.Attributes`, and use `#if` on `versionDefines` (e.g. `ELYMPICS_UNITY_PIPELINE_0_8_OR_NEWER`) where the API really differs. Verify a command through the `unity command` listing, never by running an upload command.
- **Reason:** 0.8.0-exp.1 moved the attributes into `Unity.Pipeline.Attributes` (same namespace). Projects may pin pipeline versions from 0.5 on, which overrides the package dependency. StreamingAssets uploads are write-once, and pipeline has no `dry_run` for custom commands.
- **Example:** after updating the host to 0.8, `ElympicsStreamingAssetsCliCommands.cs` failed with `CS0246 CliCommandAttribute`.
- **Time scope:** If pipeline leaves `-exp` or the supported range moves above 0.8, drop the old reference.

## Verification

### dotnet format is not a compile check
- **Rule:** never cite a `dotnet format` exit code as proof that code compiles.
- **Reason:** it checks style only.
- **Example:** it exited 0 on a `WebGLUploader.cs` that the compiler rejected with six errors (CS0201, CS0019, CS0029).
- **Time scope:** If the CI style check stops using `dotnet format`, remove this.

### Compile and test evidence
- **Rule:** compile with the redirected `dotnet build` from the host project dir (see CLAUDE.md), never a plain one. For tests:
  - If a Unity license seat is free, use batchmode `-runTests`.
  - If the host project has `com.unity.pipeline`, drive the running Editor with the Unity CLI (`unity command`).
  - Otherwise, ask the user to run Test Runner in the open Editor, and read the evidence yourself:
    - `<host>/Library/ScriptAssemblies/Elympics.Editor{,.Tests}.dll`: the mtime advances only on a successful compile.
    - `Editor.log` (Windows: `%LOCALAPPDATA%/Unity/Editor/Editor.log`) accumulates across sessions and Editor instances. Compare the byte offset of a match against the file size before trusting it.
    - `TestResults.xml` is written to `<LocalLow>/<companyName>/<productName>/` (Windows: `%USERPROFILE%/AppData/LocalLow/…`), using the names in the host's `ProjectSettings/ProjectSettings.asset`. Parse `total`/`passed`/`failed`/`result` on `<test-run>` and `result` on each `<test-case>`.
- **Reason:** a plain `dotnet build` writes into the `obj/`/`bin/` dirs Unity owns. Batchmode dies without a free license seat, and seats are usually held by the open Editor. It would also hit the `Library` lock.
- **Example:** `-runTests` fails with `BatchMode: Unity has not been activated with a valid License` before compiling anything.
- **Time scope:** If `com.unity.pipeline` becomes a dependency of this package, rewrite the test part.

### Generated csprojs follow the Editor's build target
- **Rule:** before trusting a redirected `dotnet build`, check the platform defines in the host's generated csproj (`<DefineConstants>`). If it has `UNITY_WEBGL` (or another player platform), also compile with that platform's defines swapped for the other platform, so both sides of every `#if` build. Build the references normally first and rebuild only the Elympics projects with `-p:DefineConstants=…` (semicolons escaped as `%3B`) and `-p:BuildProjectReferences=false`. A global define override also replaces the defines of vendored projects such as UniTask. Tests that depend on a platform define should take the capability as input (like `ElympicsLaunchPlatform`), not read the define.
- **Reason:** Unity generates the csprojs, and runs Editor tests, with the active build target's defines. With WebGL selected, `UNITY_WEBGL` is defined in the Editor, so the `#else` branches are never compiled by a plain redirected build, and Editor tests take the WebGL paths.
- **Example:** a `#if UNITY_WEBGL` TCP/UDP guard made two validation tests fail in the Editor, whose build target was WebGL. A global `DefineConstants` override then broke UniTask's `UnityWebRequest` extensions (CS8209/CS0246 in `GoogleCloudPingResultFactory.cs`).
- **Time scope:** If CI or the host project stops generating csprojs per build target, rewrite this.

### The host Editor may be on the Dedicated Server sub-target
- **Rule:** before changing scripting defines in an Editor, check `EditorUserBuildSettings.standaloneBuildSubtarget`. With `Server`, set them on `NamedBuildTarget.Server`; `NamedBuildTarget.Standalone` has no effect on the compile. Check what the compiler gets with `CompilationPipeline.GetAssemblies(...).defines`.
- **Reason:** the Editor compiles with the defines of its active (sub)target. A define set on the wrong target is stored and returned by `GetScriptingDefineSymbols`, so it looks applied.
- **Example:** `ELYMPICS_LAZY_WEBRTC_CONTEXT` on Standalone left `EditorWebRtcContext` out of `Elympics.WebRtc.Other.dll` in the host (active target StandaloneLinux64, sub-target Server); on `NamedBuildTarget.Server` it compiled in. CI's `set_scripting_defines.sh` writes Standalone, Server and WebGL.
- **Time scope:** If the host project stops using the Server sub-target, keep only the general rule.

### The running Editor caches EditorPrefs
- **Rule:** to force Editor state for a manual test (e.g. an expired developer token), make a temporary, uncommitted code change inside the Editor, then revert it. Don't write EditorPrefs from outside Unity.
- **Reason:** an open Editor caches EditorPrefs in memory, so an external registry write isn't seen, and the test silently checks nothing.
- **Example:** setting `AuthTokenExp` to 0 in the registry left the Editor using its cached value, and the Play-mode token refresh never ran. A temporary 1-day expiry margin in the hook forced the refresh instead.
- **Time scope:** If Unity stops caching EditorPrefs per process, remove this.

### Test Runner window runs write TestResults.xml only through the performance package
- **Rule:** when reading Editor test results from `TestResults.xml` in `Application.persistentDataPath`, make sure the host has `com.unity.test-framework.performance`. Check the file's `start-time` before trusting it.
- **Reason:** Test Framework 1.4.x saves results to a file only in command-line runs. The file after a window run comes from the performance package's `PerformanceTestRunSaver`. Without it, a window run leaves the previous file, and a watcher never fires.
- **Example:** removing `com.unity.entities` from the host also removed the performance package it pulled in, so a PlayMode run left no file, until the package was added directly.
- **Time scope:** If the host adds its own results-saving callback or the Test Framework saves window runs, remove this.

### WebGL linked assemblies: read the build, not ScriptingAssemblies.json
- **Rule:** to see which assemblies a WebGL build linked, read `Library/Bee/artifacts/WebGL/ManagedStripped` right after that build, or the `.dll` names in the IL2CPP `global-metadata.dat` packed in the build's `.data`. `ScriptingAssemblies.json` lists the assemblies before linking.
- **Reason:** `ManagedStripped` is overwritten by the next build, and `ScriptingAssemblies.json` makes stripped assemblies look linked.
- **Example:** `ScriptingAssemblies.json` listed 159 assemblies, including Nethereum and Newtonsoft. The metadata's string table matched `ManagedStripped` exactly, at 72 = 72.
- **Time scope:** If the WebGL build format changes (e.g. a newer Unity's data packing), recheck this.

### Domain reload off keeps NUnit ValueSource objects between runs
- **Rule:** tests must not mutate objects that come from `[ValueSource]`/`[TestCaseSource]`. Pass kinds or factories and create the object inside the test.
- **Reason:** NUnit builds those objects once per test tree, and with Enter Play Mode Options (Domain Reload disabled; the default for new 6.6+ projects and the CI `test:playmode:no-domain-reload` job) the tree outlives a play session.
- **Example:** `TestParameter` failed 4 cases on the second run until each case created its own `Parameter`.
- **Time scope:** Permanent while the tests use NUnit sources.

### Unity's Mono HttpListener can't accept WebSockets
- **Rule:** test WebSocket clients against a `TcpListener` echo server that does the RFC 6455 handshake itself and wraps the stream with `WebSocket.CreateFromStream`.
- **Reason:** `HttpListenerContext.AcceptWebSocketAsync` throws NotImplementedException in Unity's Mono, so the client's `ConnectAsync` waits forever without an error.
- **Example:** `TestNativeWebSocket` in PlayPad.
- **Time scope:** If tests run on CoreCLR Unity, `HttpListener` may work; recheck.

### Driving the Editor through the pipeline CLI
- **Rule:** put global flags before the subcommand (`unity --json command run_tests …`). Use `recompile` + `recompile_status`, or, when compile errors already exist (it then refuses), `CompilationPipeline.RequestScriptCompilation` via `eval` or a window minimize/restore. Keep `eval` work under ~5 s and reach internal types through reflection. For anything longer (a Project Auditor scan, a build step), have a `run_script` entry point schedule the work with `EditorApplication.delayCall`, return at once, and write its result to a file to poll. Before `build --target X`, run `switch_build_target --target X --confirm true` (poll `switch_build_target_status`). Array arguments take JSON (`--scenes '["Assets/A.unity"]'`).
- **Reason:** flags after `command` are passed to the command as parameters. `eval` runs on the main thread with a 5000 ms limit and compiles against public API only. Synchronous pipeline commands (`run_script` included) are cut off after 30 s regardless of `--timeout_ms`, and the work then never completes.
- **Example:** `unity command run_tests --mode EditMode --no-banner --json` echoed `no-banner` and `json` back among the command's parameters. A `build --target WebGL` from a Linux-active Editor compiled without `UnityEngine.WebGLModule` and failed with CS0103 `WebGLInput` in PlayPad, which builds fine after a switch.
- **Time scope:** If the pipeline CLI changes its argument handling or limits, update this.

### A newer Unity Editor rewrites package sources
- **Rule:** after opening the host in a newer Unity version, check `git status` inside both packages before committing anything, and review what the Script Updater changed.
- **Reason:** embedded packages are writable, so the API Updater rewrites obsolete calls in them on import without asking. The edits are usually right but incomplete (it skips APIs without a 1:1 replacement).
- **Example:** opening the host in 6000.3 rewrote `velocity`/`drag`/`angularDrag` in both Rigidbody synchronizers but left `Rigidbody2D.isKinematic`, which needed `bodyType` by hand.
- **Time scope:** Permanent; relevant at every minimum-version raise (next: 6.7 LTS).

## Configuration

### The active game is availableGames[0]; duplicate game IDs resolve to the first entry
- **Rule:** the launcher, the Editor tools and PlayPad use `ElympicsConfig.availableGames[0]` as the active game. When several entries share a game ID, a launch by that ID picks the first.
- **Reason:** the legacy `currentGame` index was removed. With duplicate IDs, the list order silently decides which gameplay scene loads.
- **Example:** two sample configs uploaded to the same staging game ID, and reordering `availableGames` switched which sample's gameplay scene a launch loaded.
- **Time scope:** If `ElympicsConfig` gets an explicit active-game field again, rewrite this.

## Git

### Pre-edit baseline via git show, never git stash; snapshot unstaged work before editing
- **Rule:** get old file content with `git show :path` (index) or `git show HEAD:path` (commit) into a scratch dir. Before editing a file that has unstaged changes, copy it.
- **Reason:** the working tree is often fully staged with unstaged edits on top. `git stash push --keep-index` followed by `git stash pop` then conflicts and writes conflict markers into sources. Unstaged work has no git copy, so `git checkout -- <file>` destroys it along with your edit.
- **Example:** a stash pop left `UU` conflicts in two edited files. Recovery was `git checkout stash@{0} -- <paths>` for the working tree, then `git reset stash@{0}^2 -- <paths>` for the index.

### New CI scripts need git's executable bit
- **Rule:** after adding a `.sh` that CI runs as `./script.sh`, set `git update-index --chmod=+x <file>` and check `git ls-files -s` shows `100755`.
- **Reason:** on Windows `chmod +x` doesn't reach git (`core.filemode` is off), so the file lands as `100644` and the Linux CI job fails with "permission denied".
- **Example:** `build_sample.sh`, `disable_domain_reload.sh` and PlayPad's copied `check_js.sh` all needed it.
- **Time scope:** Permanent while CI runs on Linux and development happens on Windows.
