# Shared agent memory

Important, non-obvious knowledge about this package. Use this file as your memory for this package instead of your local/auto memory.

- Add an entry when you learn something important that the code, git history or CLAUDE.md doesn't already say. Update or remove an existing entry instead of duplicating it.
- Every entry has **Rule**, **Reason**, **Example** and **Time scope** (the condition under which to rewrite or remove it). Remove or rewrite entries whose time scope has passed.
- No personal or machine-specific data (user names, home-dir paths, local tool locations).
- Entries are grouped under `## Category` headings, each entry a `### Title`. Put a new entry under the category that fits, or add a new category if none does.
- After every edit, regenerate `MEMORY.short.md` (CLAUDE.md imports it, so every character is paid on every request):
  - The same category headings, one bullet per entry: its title and the core of its Rule, at most ~120 characters. No Reason/Example/Time scope.
  - Leave out entries whose rule `CLAUDE.md` already states.
  - Situational entries (needed only in one kind of task) go under the final `## Situational (see MEMORY.md)` heading as a title-only label of at most 30 characters.

## Code

### jslib/jspre are ES2020 and call C# through makeDynCall
- **Rule:** no syntax newer than ES2020 in `.jslib`/`.jspre` files (no class fields, no `||=`; `?.` and `??` are fine). Call C# callbacks with `{{{ makeDynCall('vii', 'callback') }}}(a, b)`, never `Module.dynCall_*`. Don't put Emscripten macros in `.jspre` files. Prefix every top-level identifier and `$` library symbol with `Elympics`/`elympics`: a game's other packages may ship the same upstream library (e.g. `com.unity.services.wire`'s WebSocket.jslib).
- **Reason:** Unity 6.3–6.4 ship Emscripten 3.1.39 (acorn and Closure at ES2020; Unity 6.5+ ship 4.0.19). `tsc` doesn't reject newer syntax, so `check_js.sh` also parses with acorn. `makeDynCall` works with "Use WebAssembly.Table" on and off, and Emscripten adds `getWasmTableEntry` automatically; `.jspre` files are inserted verbatim. With the Table off, a `dynCall_<sig>` that isn't exported turns the call into a silent no-op (only assertion builds throw), so WebGL callback changes need a run with the Table off and on.
- **Example:** a canary `a ||= 1` passed `tsc` and failed the acorn step. The removed `dynamicCall.jspre` polyfilled `Module.dynCall_*` with `getWasmTableEntry`.
- **How to check callbacks:** build a scratch project's WebGL client four times (Table off/on × development/release), serve it locally, and drive it with headless Chromium (Playwright): on `ready`, call `window.elympicsHost.launchHalfRemoteClient(gameId, { serverAddress: '127.0.0.1:<web port>', playerIndex: 0, transport: 'webRtc' })` against a standalone half-remote server (`--elympics-launch-mode=halfRemoteServer`; the gameplay scene must be in its build), and expect `launchSucceeded`. WebSocket callbacks need a probe against a local echo server. On 2026-10-14 all four builds passed on 6000.3.10f1.
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
- **Rule:** read Multiplayer Play Mode state only through `PlayModeInstance` (`IsVirtualPlayer`, `Tags`, `PlayerName`), never `CurrentPlayer` directly. It reads `CurrentPlayer` only under `ELYMPICS_MPPM`, a `versionDefines` entry of `Runtime/Elympics.asmdef` set when `com.unity.multiplayer.playmode` is installed. That define exists only in the `Elympics` assembly, so MPPM code in other assemblies goes through `PlayModeInstance` too. Compile both sides of the define: CI testing projects don't have the package.
- **Reason:** in 6.3, `Unity.Multiplayer.PlayMode.CurrentPlayer` (in the built-in `UnityEditor.MultiplayerModule`, so it always compiles) throws a NullReferenceException from `IsMainEditor` and `Tags` (from `SystemDataStore.GetMain`) when the package isn't installed, which is most games. Without the define, `PlayModeInstance` returns the main Editor's answers without calling it; with it, a narrow `catch (NullReferenceException)` stays as a safeguard. The answers are process-constant and cached, which also keeps inspector repaints cheap.
- **Example:** `UsageStatisticsSender`'s `[InitializeOnLoad]` static constructor threw on Editor reload in a host without the package. A catch-all around every read used to hide that, with a throw on each reload.
- **Time scope:** If Unity stops throwing there, drop the catch. If Elympics starts depending on the package, drop the define.

### Multiplayer Play Mode virtual players run the main Editor's scenes
- **Rule:** don't assume a virtual player starts in the scene its role needs. Code that must run in a particular scene loads it itself (as `ElympicsMatchLauncherBootstrap` does for the gameplay scene whenever the launch mode parameter is set, in virtual players and builds alike). To check MPPM end to end, read the virtual players' `Library/VP/<id>/Logs/Editor.log`; the active players and tags are in `Library/VP/SystemData.json`.
- **Reason:** MPPM opens in each virtual player the scenes open in the main Editor when play mode starts, and scenes loaded during play mode aren't mirrored. A Match Launcher launch first switches the main Editor to the boot scene, so virtual players got the lobby and their launch mode parameter, which starts a match only in the gameplay scene, never fired.
- **Example:** on 2026-10-14, both virtual players logged `LaunchMode: halfRemoteClient` with indices 0/1 and then sat in the AsyncGame lobby. After the fix, both joined. The fix was later generalized to any process started with the launch mode parameter in a non-gameplay scene (a build whose first scene is a boot scene). The server waits for as many players as the game config's test players, so a 2-client check needs at least 2.
- **Time scope:** If MPPM gets per-player scene setup, revisit.

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

### com.unity.webrtc disposes every native object on Application.quitting in players
- **Rule:** code that may run during player shutdown (`Disconnect`, `Dispose`, `OnDestroy`) must tolerate `ObjectDisposedException` from `RTCDataChannel`/`RTCPeerConnection` members such as `ReadyState`, `Send` and `Close`. Don't rely on a goodbye message sent at quit reaching the peer; the server has to detect the drop itself.
- **Reason:** in players, `Context.Init` subscribes `Application.quitting` → `WebRTC.DisposeInternal()`, which disposes every object in its table, possibly before our handlers run. The Editor subscribes only `EditorApplication.quitting`, so play mode never shows it.
- **Example:** `HalfRemoteMatchConnectClient.Disconnect` threw at quit from `WebRtcDataChannel.Send` → `ReadyState` → `GetSelfOrThrow()`, skipping "Disconnected by client" (R5, 2026-10-19).
- **Time scope:** If com.unity.webrtc stops disposing on `Application.quitting`, remove this.

### In the Editor, Application.runInBackground is the project's Player setting
- **Rule:** setting `Application.runInBackground` in the Editor (also during play mode) changes `PlayerSettings.runInBackground`, which can be saved into `ProjectSettings.asset`. Say so wherever runtime code changes it.
- **Reason:** the two properties mirror each other in the Editor; it isn't a play-mode-only override.
- **Example:** `eval` setting `Application.runInBackground = false` made `PlayerSettings.runInBackground` read false at once (2026-10-19). The half-remote server enables it and warns (user decision: no restore).
- **Time scope:** If Unity separates the two in the Editor, remove this.

### BuildPlayer with a Server subtarget leaves the Editor's standalone subtarget on Server
- **Rule:** a build helper that passes `BuildPlayerOptions.subtarget` must save `EditorUserBuildSettings.standaloneBuildSubtarget` before `BuildPipeline.BuildPlayer` and restore it in `finally`.
- **Reason:** on 6000.3 `BuildPlayer` writes the subtarget into the Editor settings even when the build fails, so the next standalone player build becomes a Server build.
- **Example:** a scratch batchmode check logged `start=Player`, `raw … after=Server`, and `BuildServerLinux … after=Player` once `BuildTools.BuildServer` restored it (R3, 2026-10-19).
- **Time scope:** If Unity stops persisting the subtarget from `BuildPlayerOptions`, remove this.

### Unity 6.5+ reports unchanged scripts again after a requested recompile
- **Rule:** an `AssetPostprocessor` that calls `CompilationPipeline.RequestScriptCompilation` for some scripts in `importedAssets` must skip scripts it has already requested for (e.g. path plus write time in `SessionState`, as `Editor/Weaving/Weaver.cs` does). `didDomainReload` can't tell the two cases apart.
- **Reason:** after the domain reload that follows the requested compile, 6000.5 and 6000.6 call `OnPostprocessAllAssets` again with the same unchanged scripts (6000.4 doesn't), so each request starts a second clean compile. `didDomainReload` is true on the first, real import too. `-runTests` then runs the tests while that second compile is in progress, so `EditorApplication.isCompiling` is true.
- **Example:** on MR !604, the 6000.5/6000.6 CI jobs failed `RefusedLaunchIsRecordedAsFailed` with `ELYMPICS_COMPILING` until Weaver skipped repeat reports (2026-10-23). Two "Requested through public api" log lines in one run are the symptom.
- **Time scope:** If Unity stops re-reporting the scripts, or Weaver no longer requests compiles, remove this.

## Backend contracts

### StreamingAssets upload contract
- **Rule:** treat the per-extension Content-Type map as a verified allowlist: `.json` → `application/json`, `.xml` → `application/xml`, `.txt` → `text/plain`, everything else `application/octet-stream`. Don't add an extension without a real upload confirming it. Keep the upload order bundles → catalogs → `variants.meta.json`. Don't validate catalog URLs baked into builds.
- **Reason:** a version is write-once (re-initializing returns 400, and there's no resume), so any failed PUT uses up that version number. The upload order makes a partial version fail closed. The signed URL covers `content-type`, so a mismatch surfaces as GCS `SignatureDoesNotMatch`, not as a content-type error. The game runner overwrites the baked URLs at runtime. Bucket layout: `{bucket}/{gameId}/sa/{version}/{variant}/aa/…`.
- **Example:** sending `text/plain` for `.hash` failed with `SignatureDoesNotMatch` and used up that version number. On staging (2026-10-23), `application/octet-stream` for `.txt` failed the same way, and `text/plain` for `.txt` then worked; `.bin` with `application/octet-stream` and `.json` worked too.
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

### com.unity.pipeline 0.8 is the floor; UPM can't downgrade below a package dependency
- **Rule:** keep `com.unity.pipeline` in `package.json` at the lowest version Elympics supports, and don't design for older versions than that: a project can't pin one. `[CliCommand]`/`[CliArg]` come from `Unity.Pipeline.Attributes` (0.8+). Verify a command through the `unity command` listing, never by running an upload command.
- **Reason:** Unity 6's Package Manager treats a package's dependency version as a minimum ("Requested version X overridden by minimum version Y" in `upm.log`): a project manifest pin of 0.7 still resolved 0.8 (embedded and tarball installs, lock deleted). The command compiled against 0.5–0.8, but those versions were unreachable, so the user chose 0.8 as the floor: 0.8 binds its servers to loopback, 0.7+ keeps the Roslyn DLLs out of release players, and 0.5–0.7 likely don't compile on 6000.7. StreamingAssets uploads are write-once, and pipeline has no `dry_run` for custom commands.
- **Example:** after updating the host to 0.8, PlayPad's `PlayPadUploadCliCommands.cs` failed with `CS0246 CliCommandAttribute` until the asmdef referenced `Unity.Pipeline.Attributes`. Pipeline 0.4 lacks `CliCommandAttribute.Tags`.
- **Time scope:** If Unity's Package Manager lets projects downgrade package dependencies, revisit.

## Verification

### dotnet format is not a compile check
- **Rule:** never cite a `dotnet format` exit code as proof that code compiles.
- **Reason:** it checks style only.
- **Example:** it exited 0 on a `WebGLUploader.cs` that the compiler rejected with six errors (CS0201, CS0019, CS0029).
- **Time scope:** If the CI style check stops using `dotnet format`, remove this.

### Compile and test evidence
- **Rule:** compile with the redirected `dotnet build` from the host project dir (see CLAUDE.md), never a plain one. For tests:
  - Drive the running Editor with the Unity CLI (`unity command`; this package depends on `com.unity.pipeline`). See "Driving the Editor through the pipeline CLI".
  - If a Unity license seat is free, batchmode `-runTests` also works.
  - If neither works, ask the user to run Test Runner in the open Editor, and read the evidence yourself:
    - `<host>/Library/ScriptAssemblies/Elympics.Editor{,.Tests}.dll`: the mtime advances only on a successful compile.
    - `Editor.log` (Windows: `%LOCALAPPDATA%/Unity/Editor/Editor.log`) accumulates across sessions and Editor instances. Compare the byte offset of a match against the file size before trusting it.
    - `TestResults.xml` is written to `<LocalLow>/<companyName>/<productName>/` (Windows: `%USERPROFILE%/AppData/LocalLow/…`), using the names in the host's `ProjectSettings/ProjectSettings.asset`. Parse `total`/`passed`/`failed`/`result` on `<test-run>` and `result` on each `<test-case>`.
- **Reason:** a plain `dotnet build` writes into the `obj/`/`bin/` dirs Unity owns. Batchmode dies without a free license seat, and seats are usually held by the open Editor. It would also hit the `Library` lock.
- **Example:** `-runTests` fails with `BatchMode: Unity has not been activated with a valid License` before compiling anything.
- **Time scope:** If this package drops its `com.unity.pipeline` dependency, rewrite the test part.

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

### Project Auditor skips assemblies it can't compile; keep test-only DLLs Editor-only
- **Rule:** keep DLLs that only tests use (NSubstitute) enabled for the Editor only, never for player platforms. Before trusting a Project Auditor result or baseline, prove the target assemblies were analyzed: grep the run's log for `error CS`, and when in doubt add a canary (e.g. a mutable public static) to a scratch copy and check it shows up.
- **Reason:** the auditor recompiles each player assembly with `AssemblyBuilder`, which references every precompiled DLL compatible with the target platform, whatever its Auto Reference setting. NSubstitute 3.0 bundles its own `ValueTask<T>`, so MessagePack and UniTask failed with CS0433, and every assembly built on them was skipped without an error or a failed run. Enabled for Standalone, the DLL was also copied into games' player builds (`Game_Data/Managed/NSubstitute.dll`).
- **Example:** with the DLL enabled for Standalone, the Elympics audit found 10 issues (only small assemblies such as `Elympics.Core`) and PlayPad 0 even with a canary; Editor-only, 78 and 35, with the tests present. Turning off Auto Reference alone didn't help.
- **Time scope:** If the tests stop vendoring NSubstitute, or Project Auditor stops referencing unrelated precompiled DLLs, shorten this to the canary check.

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

### Editor API tests that can reach the backend need EditorApiScope
- **Rule:** any Editor test whose code under test could send an `ElympicsEditorApi` request, even only when a check it tests regresses, runs inside `EditorApiScope` with `FakeLoginStore` and `FakeTransport` (`Tests/Editor/EditorApiFakes.cs`).
- **Reason:** `CliAssert.ThrowsAsync` and other `[Test]` helpers block the main thread on the task. The real transport completes `UnityWebRequest`s only on the Editor's player loop, so the wait deadlocks and freezes the Editor. The Test Runner's timeout doesn't unfreeze it, so the Editor has to be killed. Without the fakes, the test also uses the developer's real login and backend.
- **Example:** on 2026-10-26, `QueueUpdateRejectsNonPositiveTeamSizes` without the fakes froze the Editor once the fix it guards was undone: `queue_update` went on to refresh the real token.
- **Time scope:** If the real transport stops depending on the player loop, or tests stop blocking on tasks, remove this.

### Scratch Editors share the developer's Elympics login and can log it out
- **Rule:** before launching scratch or batch-mode Editors with this package, point the scratch `ElympicsConfig` at a reachable web endpoint, or make sure the stored login isn't expired. Afterwards, check whether the developer is still logged in.
- **Reason:** the Elympics login (auth and refresh tokens) lives in machine-global EditorPrefs, so every Editor on the machine shares it. On each Editor start, `UsageStatisticsSender` (`[InitializeOnLoad]`) posts telemetry through `CheckAuthTokenAndRefreshIfNeeded`. When the stored token has expired and the refresh fails, that path calls `SetAsLoggedOut()`, which logs out every Editor. A successful refresh from a scratch Editor rotates the refresh token instead.
- **Example:** on 2026-10-15, scratch projects whose config pointed at a dead endpoint (`127.0.0.1:9`) logged "Auth token expired. Refreshing…" at every launch. The refresh failed, and the developer's Editor was left logged out.
- **Time scope:** If login state stops being machine-global, or a failed refresh stops clearing it, remove this.

### Driving the Editor through the pipeline CLI
- **Rule:** put `command`'s own flags (`--result-only`, `--timeout <s>`) right after `command` and before the subcommand (`unity command --result-only --timeout 300 run_tests …`); `unity --json` goes before `command`. New files only appear after `eval --code 'UnityEditor.AssetDatabase.Refresh(); return "ok";'` (then poll `recompile_status`). `run_tests --filter_type` takes `testName` (namespace/class prefix), `assembly` or `category`, not regex. PlayMode tests need `--async_tests true` and polling `test_status` (its result uses lowercase `summary`/`results`). Use `recompile` + `recompile_status`, or, when compile errors already exist (it then refuses), `CompilationPipeline.RequestScriptCompilation` via `eval` or a window minimize/restore. Keep `eval` work under ~5 s and reach internal types through reflection. For anything longer (a Project Auditor scan, a build step), have a `run_script` entry point schedule the work with `EditorApplication.delayCall`, return at once, and write its result to a file to poll. Before `build --target X`, run `switch_build_target --target X --confirm true` (poll `switch_build_target_status`). Array arguments take JSON (`--scenes '["Assets/A.unity"]'`). Always set `UNITY_PROJECT_PATH` (or `--project-path`) on every call: discovery fails with more than one Editor open, and also when Unity Hub started the Editor (`-projectpath` in lowercase), even though the server is running. Treat a test run as evidence only when its summary total is above 0.
- **Reason:** flags after `command` are passed to the command as parameters. `eval` runs on the main thread with a 5000 ms limit and compiles against public API only. Synchronous pipeline commands (`run_script` included) are cut off after 30 s regardless of `--timeout_ms`, and the work then never completes.
- **Example:** `unity command run_tests --mode EditMode --no-banner --json` echoed `no-banner` and `json` back among the command's parameters. A `build --target WebGL` from a Linux-active Editor compiled without `UnityEngine.WebGLModule` and failed with CS0103 `WebGLInput` in PlayPad, which builds fine after a switch.
- **Example (2026-10-19):** `unity --result-only command …` and `unity --timeout 120 command …` failed with "unknown option"; a synchronous PlayMode `run_tests` returned "entering play mode triggers a domain reload that drops the request".
- **Example (2026-10-23):** with a second project's Editor open, every call failed with a list of candidate projects. A wrapper script still exited 0, with null compile and test results, so a run that tested nothing looked like a pass.
- **Example (2026-10-27):** with one Hub-launched Editor (CLI 1.0.0-beta.13, pipeline 0.8.0-exp.1), `unity status` and `unity pipeline list` listed nothing, although `Library/Pipeline/.unity-pipeline-port` was fresh and the server was listening on 7800. With `UNITY_PROJECT_PATH` set, `run_tests` worked.
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
