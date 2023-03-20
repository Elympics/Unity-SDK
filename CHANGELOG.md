## [0.5.3](https://github.com/Elympics/Unity-SDK/compare/v0.5.2...v0.5.3) (2023-03-20)


### Features

* introduction to #LOG_TO_FILE preprocessor directive that allows writing Elympmics simulation data logs into the file ([9cdc0f4](https://github.com/Elympics/Unity-SDK/commit/9cdc0f4f2b70fcd63614da85dd8b8de4f164c587))
* Elympics Markers for Unity Profiler added ([184fb54](https://github.com/Elympics/Unity-SDK/commit/184fb54ba53a060d27d940f166247def12004f44))


## [0.5.2](https://github.com/Elympics/Unity-SDK/compare/v0.5.1...v0.5.2) (2023-03-06)


### Bug Fixes 🪲

* NetworkId will not generate IDs above the declared limit ([11ca5f0d](https://github.com/Elympics/Unity-SDK/commit/11ca5f0de9ce397d4845ece84c56d2bad5906451))
* If the NetworkId limit will be overflown, the NetworkID generator will reiterate again through min-max limits to find the available IDs. Throw exceptions when failed to do so. ([11ca5f0d](https://github.com/Elympics/Unity-SDK/commit/11ca5f0de9ce397d4845ece84c56d2bad5906451))
* Log server errors to stderr ([fe383b6d](https://github.com/Elympics/Unity-SDK/commit/fe383b6d0e5a6a065f49d16438db7d30cb8fd73d))


## [0.5.1](https://github.com/Elympics/Unity-SDK/compare/v0.5.0...v0.5.1) (2023-02-14)


### Bug Fixes 🪲

* Increase TCP and WebRTC session timeout, refactor GameServerClient ([890e89e](https://github.com/Elympics/Unity-SDK/commit/890e89e9cbf62177dd9667b6c791f88128204971))

### Features

* Make client connection timings configurable from Elympics Game Config ([890e89e](https://github.com/Elympics/Unity-SDK/commit/890e89e9cbf62177dd9667b6c791f88128204971))


## [0.5.0](https://github.com/Elympics/Unity-SDK/compare/v0.4.9...v0.5.0) (2023-02-07)


### Bug Fixes 🪲

* Make sure Elympics-SDK-Version HTTP header is not empty ([1bbb97d](https://github.com/Elympics/Unity-SDK/commit/1bbb97df800d8ea1d457a99d66612912d745aced))

### Features

* Implement Networked Simulation Analyzer ([42c0043](https://github.com/Elympics/Unity-SDK/commit/42c00437185f3ee5473904b58a18f24c986e9b5d))


## [0.4.10](https://github.com/Elympics/Unity-SDK/compare/v0.4.9...v0.4.10) (2023-01-31)


### Features

* Allow for specifying queue and region for Debug Online mode ([d1988ab](https://github.com/Elympics/Unity-SDK/commit/d1988ab50b3ed8075777e201443394af3fac7adf))
* Print an error when Half Remote server is run with `Application.RunInBackground` set to `false` ([c76b2d2](https://github.com/Elympics/Unity-SDK/commit/c76b2d2781ebdce80fffe5a1941f9d317d853b8e))
* Include the player's input in the snapshot data ([1cc4712](https://github.com/Elympics/Unity-SDK/commit/1cc471240315ae8702ccb4fdebeeda2a628838f4))



## [0.4.9](https://github.com/Elympics/Unity-SDK/compare/v0.4.8...v0.4.9) (2023-01-09)

### Changed

- `ElympicsLobbyClient.Instance.PlayOnline` now accepts a `CancellationToken` that can be used to interrupt the matchmaking process.

### Bug Fixes 🪲

- Improved support for new API endpoint format. Original path segment is no longer truncated by HTTP client.


## [0.4.8](https://github.com/Elympics/Unity-SDK/compare/v0.4.7...v0.4.8) (2022-12-20)

### Features

- Possibility of joining region-specific matchmaking queues.  
  See new `regionName` argument in `ElympicsLobbyClient.Instance.PlayOnline()`.  
  Empty/null region name means that servers based in Warsaw are used (legacy behavior).  
  Regions configured for active game can be retrieved using "Synchronize" button in "Manage games in Elympics" window.


## [0.4.7](https://github.com/Elympics/Unity-SDK/compare/v0.4.6...v0.4.7) (2022-12-14)

### Features

- Possibility of reusing old input in case current input does not reach the game server in time.  Check out `inputAbsenceFallbackTicks` argument of `TryGetInput` method described [here](https://docs.elympics.cc/guide/inputs/#reading-inputs).
- Inspector warning when using `IUpdatable` for non-predictable behaviors.

### Bug Fixes 🪲

- Fixed a bug causing the client to stop prediction when the latest received snapshot tick was greater than the latest predicted tick.
- Streamlined building and uploading a server to the cloud.
- Fixed a bug that could occur when creating Elympics asset directory using "Create first game config!" button.
- Fixed possible zero division error in ElympicsBehaviourStateChangeFrequencyCalculator.

## [0.4.6](https://github.com/Elympics/Unity-SDK/compare/v0.4.5...v0.4.6) (2022-10-25)

### Features

- Warnings for non-synchronized private `ElympicsVar`s in base classes.
- Warnings for `Transform` and `Rigidbody` synchronizers being utilized together.
- Links to Elympics resources in package details (in Package Manager view).
- Stacktrace details in synchronized server logs (for Error, Exception and Assert levels).

### Bug Fixes 🪲

- Fix `ValueChanged` for elements of `ElympicsList`s and `ElympicsArray`s not being called.
- Prevent unnecessary reconciliations on `ElympicsAnimatorSynchronizer`.
- Make server logs not synchronized by default (to save bandwidth on production builds).
- Improve auto-generation of network IDs.
- Bring back serialization of the current value of `ElympicsVar`s.


## [0.4.5](https://github.com/Elympics/Unity-SDK/compare/v0.4.4...v0.4.5) (2022-09-27)

### Known issues :stop_sign:

- `ValueChanged` isn't called for elements of `ElympicsList`s and `ElympicsArray`s.
- Synchronized server logs aren't printed to console.

### BREAKING CHANGES :warning:

- Execution of `ValueChanged` event has been postponed until all the state is deserialized and consistent.

### Bug Fixes 🪲

- Sort behaviours implementing `IInitializable.Initialize` by their network ID, ensuring correct initialization order.
- Fix a bug with duplicated user ID in *Test players* data.
- Remove leftover warnings visible when building games.
- Make "IP Address of server" field visible again on a half-remote server.
- Allow for joining online matches through `ElympicsLobbyClient` in cloned Unity Editors.
- Make "Manage games in Elympics" accept the new format of API URL.
- Prevent `OverflowException` from being thrown in `ElympicsIntEqualityComparer`.


## [0.4.4](https://github.com/Elympics/Unity-SDK/compare/v0.4.3...v0.4.4) (2022-08-29)

### Features

- A new sample demonstrating match-related events and actions.
- `TicksPerSecond` property exposed through `IElympics` interface.

### Bug Fixes 🪲

- Fix `IClientHandler.OnMatchEnded()` not being called when a match ends. Also, fix `IClientHandler.OnAuthenticated()` and `IClientHandler.OnAuthenticatedFailed()` not being called when a spectator authenticates (or fails to do so).
- Allow for disabling `DefaultServerHandler` using a checkbox in Inspector.
- Make game settings from "Manage games in Elympics" window persist.
- Prevent an exception from being thrown when two or more `ElympicsInstantiate()` calls are present in a single `ElympicsUpdate()` or `Initialize()`.


## [0.4.3](https://github.com/Elympics/Unity-SDK/compare/v0.4.2...v0.4.3) (2022-08-22)

### Changes

* Share `ElympicsInstantiate` call context between all Elympics Behaviours, allowing for cross-Behaviour calls.

## [0.4.2](https://github.com/Elympics/Unity-SDK/compare/v0.4.1...v0.4.2) (2022-08-11)

### Features

- Synchronization of `isKinematic` property of `Rigidbody` and `Rigidbody2D` in corresponding Elympics synchronizers.

### Changes

- Allow for running `ElympicsInstantiate` and `ElympicsDestroy` in `Initialize` method (when implementing `IInitializable` interface).
- Make `ElympicsMonoBehaviour` implement `IObservable`.

### Bug Fixes 🪲

- Fix `NullReferenceException` being thrown in `ElympicsAnimatorSynchronizer.Update`.
- Prevent rare `UnassignedReferenceException` from being thrown when game is run in Unity Editor for the first time after scripts are recompiled.
- Fix `NullReferenceException` occuring when using an `ElympicsList` of `ElympicsGameObject`s.
- Guarantee the order of execution of `OnServerInit` and `OnPlayerConnected` methods from `IServerHandler` interface.


## [0.4.1](https://github.com/Elympics/Unity-SDK/compare/v0.4.0...v0.4.1) (2022-07-25)

### Known issues :stop_sign:

- `ElympicsAnimatorSynchronizer` may throw a `NullReferenceException` if trigger parameters are synchronized and its `Update` is run before `Initialize`.

### Fixes

- Make the new debug network logs (introduced in 0.4.0) optional, disabled by default.


## [0.4.0](https://github.com/Elympics/Unity-SDK/compare/v0.3.2...v0.4.0) (2022-07-22)

### Known issues :stop_sign:

- `ElympicsAnimatorSynchronizer` may throw a `NullReferenceException` if trigger parameters are synchronized and its `Update` is run before `Initialize`.

### BREAKING CHANGES :warning:

- `ElympicsUpdate` is no longer run on clients with prediction turned off.

### Features

- `Tick` property storing current tick number. The property is available on `IElympics` interface (accessible through `ElympicsMonoBehaviour.Elympics`).
- Debugging messages regarding current latency, differences between server time and client time, etc.

### Changes

- `ElympicsUpdate` is no longer run on clients with prediction turned off.
- Ticking mechanism in the server engine improved
- Tick number approximation improved
- More general improvements (to be extended).


## [0.3.2](https://github.com/Elympics/Unity-SDK/compare/v0.3.1...v0.3.2) (2022-07-14)

### Known issues :stop_sign:

- `ElympicsAnimatorSynchronizer` may throw a `NullReferenceException` if trigger parameters are synchronized and its `Update` is run before `Initialize`.

### Bug Fixes 🪲

* Fix build failing on `Runtime\BehaviourPredefined\AnimatorSynchronizerNamedSynchronizationStatus.cs` file.


## [0.3.1](https://github.com/Elympics/Unity-SDK/compare/v0.3.0...v0.3.1) (2022-07-11)

### Known issues :stop_sign:

- Build fails on `Runtime\BehaviourPredefined\AnimatorSynchronizerNamedSynchronizationStatus.cs` file.
- `ElympicsAnimatorSynchronizer` may throw a `NullReferenceException` if trigger parameters are synchronized and its `Update` is run before `Initialize`.

### BREAKING CHANGES :warning:

- `ElympicsUpdate` for behaviours that are not predictable for a client will no longer be executed during prediction for that client.

### Changes

- Skip `ElympicsUpdate`s of non-predictable `ElympicsBehaviour`s on clients. Provide a checkbox ("Updatable for others") in Elympics Behaviour editor for restoring the legacy behavior per game object if needed.
- Change how `ElympicsAnimatorSynchronizer` settings are serialized.

### Bug Fixes 🪲

- Fix `ElympicsAnimatorSynchronizer` not preserving state if used in a prefab instance.
- Move the network ID of Physics simulator after all objects instantiated at runtime.

### Other
- Update sample projects.


## 0.3.0 (2022-06-22)

### Changes

- Remove `IInputHandler.ApplyInput` method declaration and provide `ElympicsBehaviour.TryGetInput` which can be used to retrieve the input in `IUpdatable.ElympicsUpdate` instead.
- Rename `GetInputForClient` and `GetInputForBot` (from `IInputHandler` interface) to `OnInputForClient` and `OnInputForBot` respectively.
- Forbid usage of more than one `IInputHandler` implementation in a single game object.

### Other
- Update sample projects.
