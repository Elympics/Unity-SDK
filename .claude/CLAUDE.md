# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Shared memory

Use `.claude/MEMORY.md` as your memory for this package, instead of your local/auto memory. Append important findings there, following the format rules at its top (including regenerating `MEMORY.short.md`). The short version is imported below. Open `MEMORY.md` for details before acting on an entry.

@MEMORY.short.md

## What this is

`com.daftmobile.elympics` is the Elympics Unity SDK: a server-authoritative, deterministic-tick multiplayer netcode UPM package. It targets Unity 2021.3 and is published to GitHub as `Elympics/Unity-SDK`, with the version in `package.json`.

This repo is **only the package**. It doesn't contain a Unity project. A host Unity project uses it, either installed locally (embedded in `<project>/Packages/` or referenced with `file:` in `Packages/manifest.json`) or through UPM (cached in `<project>/Library/PackageCache/`). The host project's location isn't fixed, so find it by searching up from the package root for a dir that contains `ProjectSettings/` and `Packages/manifest.json`. If no parent matches (for example, a `file:` reference from outside the project), ask the user. The host project owns `Library/`, the generated `*.csproj`/`*.sln` and the Editor.

## Build, lint, test

Unity owns compilation, so never run a plain `dotnet build` that writes into the host project's `obj/`/`bin/`.

- **Compile check without the Editor:** run from the host project dir (found as described above), with outputs redirected to a scratch dir:
  ```
  dotnet build Elympics.Editor.csproj -p:BaseIntermediateOutputPath=<scratch>/obj/ -p:OutputPath=<scratch>/bin/ -v q --nologo
  ```
  - Build `Elympics.Editor.Tests.csproj` (or `Elympics.Tests.csproj`) to check that tests bind too.
  - Grep the output for `error CS`. `MSB3277` System.Net.Http conflicts from `Elympics.WebRtc.Other` are pre-existing noise.
- **Formatting (CI `verify-formatting`):** `dotnet format <csproj> --no-restore --verify-no-changes --severity warn [--include <file>]`.
  - It uses `.editorconfig`, and IDE-style warnings fail CI.
  - This checks style only. It does **not** prove the code compiles.
- **JS check (CI `verify-js`):** `bash .scripts/ci/check_js.sh`.
  - It copies every `.jslib`/`.jspre` (outside `Samples~`) to a temp `.js` and runs strict `tsc -p jsconfig.json --noEmit`.
  - JS must stay **ES2017** (Unity 2021.3 Emscripten): no `?.`, no `??`, no class fields.
  - Emscripten globals are declared in `Runtime/Plugins/emscripten-globals.js`.
- **Tests:** NUnit through the Unity Test Framework.
  - Locally, run them with Test Runner in the open Editor (EditMode / PlayMode). Batchmode needs a free Unity license seat.
  - CI runs `.scripts/ci/test.sh`:
    ```
    unity-editor -projectPath <proj> -runTests -testPlatform editmode|playmode -testResults <xml> -batchmode -nographics
    ```
  - To run a single test, add `-testFilter <FullyQualifiedName|regex>`.
  - CI also compiles with `ELYMPICS_DEBUG` defined (`run_with_defines.sh`).
- **Generated code must match its generator.** CI regenerates it and diffs, so re-run the tool instead of hand-editing:
  - `Runtime/Plugins/MessagePackGenerated.cs` comes from `mpc -i Elympics.csproj -o …` (MessagePack.Generator 2.5.124). Regenerate it whenever a `[MessagePackObject]` type changes.
  - `Runtime/GameEngine/Libraries/Proto/ProtosCompiled/*.cs` comes from running `.compile_protos.sh` in `Proto/Protos/` (protoc 34.1).

## Architecture

### Assemblies
The code is split across many asmdefs. The main ones:
- `Elympics` (Runtime)
- `Elympics.Core`
- `Elympics.Editor`, `Elympics.Editor.Weaving`, `Unity.Elympics.Editor.CodeGen`
- `Elympics.Editor.UnityCli` (compiles only when `com.unity.pipeline` is present)
- `GameEngineCore`, `Elympics.Proto`, `Elympics.MatchConnection`, `UnityConnectors`
- WebRtc: `Elympics.WebRtc`, `Elympics.WebRtc.Common`, `Elympics.WebRtc.Other`, `Elympics.WebRtc.WebGl`

Two rules when a file starts using another assembly's types:
- `InternalsVisibleTo` (in each `AssemblyInfo.cs`) is **not** enough. The consuming `.asmdef` `references` must also include the source assembly, usually by GUID.
- Watch for `var`-inferred types from factories. A file can depend on an assembly without ever naming its types.

### Mode selection
Matches are started by `ElympicsMatchLauncher` (`Runtime/Launcher/`).
- `ElympicsMatchLauncherBootstrap` creates it before the first scene loads, after initializing `ApplicationParameters` (env vars, command-line args, the WebGL URL query and ParrelSync clone arguments; see `Runtime/Util/Parameters/`).
- Unless the process is passive (online server or `ELYMPICS_LAUNCH_MODE` set), the bootstrap also starts a host bridge (`Runtime/Launcher/Bridges/`: WebGL or native) and emits `ready`. A host, game code (`Launch*Async`) or the Editor's Match Launcher window then launches matches. Each launch loads the gameplay scene and returns to the boot scene afterwards.

`GameSceneManager.ChooseDriver` (`Runtime/SceneManagement/GameSceneManager.cs`) picks what initializes a loaded gameplay scene. It checks in this order:
1. **Online server:** set by `ApplicationParameters`.
2. **Launch mode parameter:** `ELYMPICS_LAUNCH_MODE` runs one match in this process and quits after it (`LaunchDataFromParameters`, `ExitAfterMatchSink`). ParrelSync clones get it from their default arguments (`Editor/ParrelSync/ElympicsCloneArguments.cs`) and run as half-remote clients.
3. **Session:** the launcher's current launch. Its `LaunchData` (`Runtime/Launcher/LaunchData/`) creates the mode's `GameSceneInitializer`.
4. **Suppressed:** the scene activated while its launch is being torn down.
5. **None:** the scene waits for a launch.

Configuration lives in the `ElympicsConfig` ScriptableObject (`Resources/Elympics/ElympicsConfig`), which holds a list of `ElympicsGameConfig` assets (tick rate, half-remote endpoints, lag simulation).

### Tick loop
All of this is in `Runtime/ElympicsSystems/`.
- **Timing:** `ElympicsBase` is subclassed by `ElympicsClient` and `ElympicsServer`. There is no bot class: bots run inside the server (`IsBot`, `PlayerHandlers/`). `ElympicsBase.Update()` runs a fixed-timestep accumulator. Each step calls `ElympicsFixedUpdate` then `ElympicsLateFixedUpdate`, and each frame ends with `ElympicsRenderUpdate(alpha)`.
- **Server tick:**
  1. Take the buffered inputs for the tick, or reuse the last one.
  2. Run RPCs, then `CommitVars`, then `ElympicsUpdate`.
  3. Build the snapshot.
  4. Pass it through `Runtime/Replication/ReplicationPipeline.cs`: change detection, interest management, prioritization, bandwidth scheduling, ack tracking, encoding.
- **Client tick:**
  1. Merge the received snapshot and reconcile against `Runtime/Prediction/PredictionBuffer.cs`.
  2. Pick the predicted tick (`ClientTickCalculator`).
  3. Send input.
  4. Apply the unpredictable state.
  5. Run RPCs, then `CommitVars`, then predicted input, then `ElympicsUpdate`.
  6. Store the predicted snapshot.
- **Game-facing API:**
  - `ElympicsMonoBehaviour` and `ElympicsBehaviour` (`Runtime/Behaviour/`).
  - Synced state: `ElympicsVar<T>`, `ElympicsList`, `ElympicsArray`, `ElympicsRandom`.
  - Callback interfaces in `Runtime/BehaviourPredefined/Interfaces/`: `IInputHandler`, `IUpdatable`, `IInitializable`, `IReconciliationHandler`, `IStateSerializationHandler`, `I{Server,Client,Bot}HandlerGuid`. All derive from the marker `IObservable`.

### Serialization and weaving
- **MessagePack:** inputs, snapshots, RPCs, match data.
- **Protobuf:** the game-engine host ↔ Unity process, NTP and logs.
- **RPCs** are implemented by IL weaving:
  - `Editor/Weaving/CodeGen/ElympicsILPostProcessor.cs` is an ILPostProcessor built on Mono.Cecil.
  - It validates `[ElympicsRpc]` methods: instance, non-virtual, void, non-generic, not overloaded, optional `RpcMetadata` parameter only.
  - It then injects capture/dispatch code into them (`Editor/Weaving/Components/Elympics/ElympicsRpcComponent.cs`).
  - Woven assemblies are marked with `ProcessedByElympicsAttribute`.

### Transport
Under `Runtime/GameEngine/Libraries/`:
- `Match/MatchTcpLibrary` provides TCP/UDP/WebRTC data channels, with the interfaces in `Elympics.MatchConnection`.
- `MatchTcpClients` holds the game-server clients.
- `UnityConnectors/HalfRemote` covers editor half-remote play.
- WebRTC `Other` (non-WebGL, uses `com.unity.webrtc`) and `WebGL` (browser, through `webrtc.jslib`) define classes with the **same names**. That lets `WebRtcFactory` stay free of `#if`, so keep the two implementations API-identical.

### Editor
- `ElympicsWebIntegration.cs`: login, game versions, uploads.
- `WebGLUploader.cs`: client builds and StreamingAssets bundles.
- Custom inspectors and drawers, some in UXML/UI Toolkit.
- `SceneNetworkIdAssigner`, `Replay/`.

### Tests
- **`Tests/Editor`:** RPC weaving validation, endpoint and file checks.
- **`Tests/Runtime`:** runs in the editor only (all player platforms excluded).
  - Uses NUnit and NSubstitute. Runtime internals are visible to `DynamicProxyGenAssembly2` so they can be mocked.
  - Shared mocks and asserts are in `Tests/Runtime/Common/` and `Tests/Runtime/Util/`.
- **`Tests/Runtime/AssemblyCommunicator`:** deliberately references only `Elympics.Core`, to exercise the cross-assembly event API from outside.

### Scripting defines
`Runtime/Util/ScriptingSymbols.cs` wraps these defines:
- `ELYMPICS_DEBUG` / `ELYMPICS_TRACE`: `[Conditional]` logging.
- `ELYMPICS_PRODUCTION`: strips dev-time checks.
- `UNITY_WEBGL && !UNITY_EDITOR`, `UNITY_SERVER`, `ELYMPICS_UNITY_PIPELINE`.

## Conventions

- **Vendored / generated code, do not edit:**
  - `Runtime/Plugins/`: UniTask, MessagePack, ParrelSync, Google.Protobuf, websocket-sharp, etc.
  - `MessagePackGenerated.cs`, `ProtosCompiled/`.
  - `Elympics.Analyzers.dll`.
- **Unity `.meta` files:** every new asset or folder needs one, and renames or moves must carry the `.meta` along so the GUID is kept.
- **Public API:** from 1.0.0 on, `[PublicAPI]`-marked members are shipped API. Don't change their signatures or behaviour when refactoring. Add a sibling member instead. Before 1.0.0 (current version: `package.json`), refactoring and breaking changes are allowed, but a breaking change must increase the minor version number.
- **Commits:** Conventional Commits (`feat:`, `fix:`, `chore:`, `refactor:`, `perf:`, …).
  - `CHANGELOG.md` and the version are generated from commits on `release/vX.Y.Z` branches.
  - CI rejects any `tmp:` commit on top of `develop`.
  - Feature branches target `develop`.
- **Versions:** the version is kept in `package.json` and in `AssemblyVersion` in several `AssemblyInfo.cs` files (Runtime, Editor, Editor/Weaving, Tests/Runtime). `.scripts/ci/bump_version_and_generate_changelog.sh` updates them all. Don't bump by hand.

## Releasing

The full process is in the GitLab wiki page "Contributing and releasing". `develop` and `main` are protected, so every merge goes through an MR. Release the SDK before any PlayPad release that needs its new API.

1. **Cut the branch:** `release/vX.Y.Z` from `develop` (`hotfix/vX.Y.Z` from `main`). Pick the version by semver, keeping in mind that before 1.0.0 a breaking change raises the minor version. Pushing the branch mirrors it to GitHub.
2. **Let CI bump:** `create-changelog` commits `chore: Bump version and add changelog for version X.Y.Z` as Elympics Bot, updating `package.json`, the `AssemblyInfo.cs` files and `CHANGELOG.md`. Pull it. Never create the version or the tag by hand.
3. **Fix the changelog:** commit edits as `chore: update changelog`. If nothing needs editing, push an empty `trigger pipeline` commit instead. Release branches can't be force-pushed, so fixes go on as new commits. Things the generator gets wrong:
   - `type!:` commits land in a group without a heading. Move them into Features, Bug Fixes or Code Refactoring.
   - BREAKING CHANGES notes keep commit trailers such as `Co-Authored-By`, and footers that later commits superseded. Rewrite the notes to describe the final state.
   - Removals made with `chore:`/`refactor:` commits that have no `BREAKING CHANGE` footer don't show up at all.
   - To preview the entry before pushing, run `npx conventional-changelog-cli@2.2.2 -p angular -i CHANGELOG.md -s -c .conventional-changelog-context.json -n .conventional-changelog-config.js` in a full-history clone with `package.json` set to the new version.
4. **Merge into `main`:** open an MR from the release branch to `main` and merge it with the Merge button, without squashing and without editing the message.
5. **Publish:** on the `main` pipeline, trigger the manual `release:gitlab` job. It creates the tag and the GitLab release from the `CHANGELOG.md` entry, extracted with `.scripts/ci/parse_changelog.js` and `changelog-parser@3`. The tag pipeline then runs `release:github` (needs `GH_TOKEN`/`GH_REPO`). If it fails because the tag hasn't reached GitHub yet, retry it.
6. **Merge back:** open an MR from `main` to `develop` and merge it.
