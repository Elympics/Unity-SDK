# Architecture details

Read before changing launch/mode selection, the tick loop, serialization/weaving, transport, Editor tooling or tests. Assemblies and scripting defines are in `CLAUDE.md`.

## Mode selection
Matches are started by `ElympicsMatchLauncher` (`Runtime/Launcher/`).
- `ElympicsMatchLauncherBootstrap` creates it before the first scene loads, after initializing `ApplicationParameters` (env vars, command-line args, the WebGL URL query and Multiplayer Play Mode virtual-player arguments; see `Runtime/Util/Parameters/`).
- Unless the process is passive (online server or `ELYMPICS_LAUNCH_MODE` set), the bootstrap also starts a host bridge (`Runtime/Launcher/Bridges/`: WebGL or native) and emits `ready`. A host, game code (`Launch*Async`) or the Editor's Match Launcher window then launches matches. Each launch loads the gameplay scene and returns to the boot scene afterwards.

`GameSceneManager.ChooseDriver` (`Runtime/SceneManagement/GameSceneManager.cs`) picks what initializes a loaded gameplay scene. It checks in this order:
1. **Online server:** set by `ApplicationParameters`.
2. **Launch mode parameter:** `ELYMPICS_LAUNCH_MODE` runs one match in this process and quits after it (`LaunchDataFromParameters`, `ExitAfterMatchSink`). Multiplayer Play Mode virtual players get it by default (`PlayModeArguments`, with `Elympics:` player tags as overrides) and run as half-remote clients. When the process starts in another scene (a build's boot scene, or the main Editor's scenes that MPPM gives virtual players), the bootstrap loads the gameplay scene first (`ElympicsMatchLauncherBootstrap.LoadGameplaySceneForLaunchMode`).
3. **Session:** the launcher's current launch. Its `LaunchData` (`Runtime/Launcher/LaunchData/`) creates the mode's `GameSceneInitializer`.
4. **Suppressed:** the scene activated while its launch is being torn down.
5. **None:** the scene waits for a launch.

Configuration lives in the `ElympicsConfig` ScriptableObject (`Resources/Elympics/ElympicsConfig`), which holds a list of `ElympicsGameConfig` assets (tick rate, half-remote endpoints, lag simulation).

## Tick loop
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

## Serialization and weaving
- **MessagePack:** inputs, snapshots, RPCs, match data.
- **Protobuf:** the game-engine host ↔ Unity process, NTP and logs.
- **RPCs** are implemented by IL weaving:
  - `Editor/Weaving/CodeGen/ElympicsILPostProcessor.cs` is an ILPostProcessor built on Mono.Cecil.
  - It validates `[ElympicsRpc]` methods: instance, non-virtual, void, non-generic, not overloaded, optional `RpcMetadata` parameter only.
  - It then injects capture/dispatch code into them (`Editor/Weaving/Components/Elympics/ElympicsRpcComponent.cs`).
  - Woven assemblies are marked with `ProcessedByElympicsAttribute`.

## Transport
Under `Runtime/GameEngine/Libraries/`:
- `Match/MatchTcpLibrary` provides TCP/UDP/WebRTC data channels, with the interfaces in `Elympics.MatchConnection`.
- `MatchTcpClients` holds the game-server clients.
- `UnityConnectors/HalfRemote` covers editor half-remote play.
- WebRTC `Other` (non-WebGL, uses `com.unity.webrtc`) and `WebGL` (browser, through `webrtc.jslib`) define classes with the **same names**. That lets `WebRtcFactory` stay free of `#if`, so keep the two implementations API-identical.

## Editor
- `ElympicsWebIntegration.cs`: login, game versions, uploads.
- `WebGLUploader.cs`: client builds and StreamingAssets bundles.
- Custom inspectors and drawers, some in UXML/UI Toolkit.
- `SceneNetworkIdAssigner`, `Replay/`.

## Tests
- **`Tests/Editor`:** RPC weaving validation, endpoint and file checks.
- **`Tests/Runtime`:** runs in the editor only (all player platforms excluded).
  - Uses NUnit and NSubstitute. Runtime internals are visible to `DynamicProxyGenAssembly2` so they can be mocked.
  - Shared mocks and asserts are in `Tests/Runtime/Common/` and `Tests/Runtime/Util/`.
- **`Tests/Runtime/AssemblyCommunicator`:** deliberately references only `Elympics.Core`, to exercise the cross-assembly event API from outside.

