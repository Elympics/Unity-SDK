## [0.7.2](https://github.com/Elympics/Unity-SDK/compare/v0.7.1...v0.7.2) (2023-06-22)


### Bug Fixes 🪲

* Add a delay before posting WebRTC offer again ([70845eb](https://github.com/Elympics/Unity-SDK/commit/70845eb489d74c524d731d7607101124d842313b))
* allow for specifying queue and region for Debug Online mode ([d1988ab](https://github.com/Elympics/Unity-SDK/commit/d1988ab50b3ed8075777e201443394af3fac7adf))
* Backport changes to TCP/WebRTC session to Unity 2019.4 ([2d48501](https://github.com/Elympics/Unity-SDK/commit/2d48501eb4fd90a09505dfe3db0f47cf48f1102b))
* Change duplicated parameter name ([13e82db](https://github.com/Elympics/Unity-SDK/commit/13e82db713c4e0def47e4d53c7508276bf779648))
* choose closest region did not returned valid region ([4db965f](https://github.com/Elympics/Unity-SDK/commit/4db965f911161bf08f387e0e69b838e9c0716634))
* Correct GamesResponseModel ([dd8dff9](https://github.com/Elympics/Unity-SDK/commit/dd8dff9ac7bb2058b13fcc6b41f848d20d8bda22))
* Explicitly specify TLS version when using WebSockets ([188c431](https://github.com/Elympics/Unity-SDK/commit/188c431ff57adfb4137dd18bfdf3147f6003b3c5))
* Fix IServerHandlerGuid methods not being called in ElympicsBehaviour ([eef5ff2](https://github.com/Elympics/Unity-SDK/commit/eef5ff2b77685c948eaf08e33394320dc4c4c473))
* Fix OnSynchronized not being called on WebGL builds ([d447df4](https://github.com/Elympics/Unity-SDK/commit/d447df46452d44365347335ae462de00174d09f3))
* Gracefully handle null game and match IDs received in messages from game server ([537f650](https://github.com/Elympics/Unity-SDK/commit/537f65009b3afdb1ce57aa373e70300e76fd4e5c))
* internalized unity connectors to not interfere with external libraries ([eea1277](https://github.com/Elympics/Unity-SDK/commit/eea12776ae4ae792f263e7356ff2de47a4e4d5e5))
* Log Error Message when HalfRemote mode game is run with Application.RunInBackround set to false. ([c76b2d2](https://github.com/Elympics/Unity-SDK/commit/c76b2d2781ebdce80fffe5a1941f9d317d853b8e))
* Log server errors to stderr ([fe383b6](https://github.com/Elympics/Unity-SDK/commit/fe383b6d0e5a6a065f49d16438db7d30cb8fd73d))
* Make AuthData public and check for null in GET requests ([569bce4](https://github.com/Elympics/Unity-SDK/commit/569bce485531e146d34868a9cb8700941b3a0473))
* Make sure Elympics-SDK-Version HTTP header is not empty ([1bbb97d](https://github.com/Elympics/Unity-SDK/commit/1bbb97df800d8ea1d457a99d66612912d745aced))
* NetworkId generator refactor ([11ca5f0](https://github.com/Elympics/Unity-SDK/commit/11ca5f0de9ce397d4845ece84c56d2bad5906451))
* Put correct paths in UXML files of Networked Simulation Analyzer ([46b22ce](https://github.com/Elympics/Unity-SDK/commit/46b22ce758ae5a8eb5e9d836378eb582f569af75))
* Reload game config before starting game through ElympicsLobbyClient ([d3ae1b0](https://github.com/Elympics/Unity-SDK/commit/d3ae1b0975752e72f9b87d90fb3026ca3b5509a1))
* Remove superfluous Assembly Definitions ([953640e](https://github.com/Elympics/Unity-SDK/commit/953640e35053e1be89321acb6b4778b99ee091c0))
* Replace ContinueWith calls with try-catch blocks ([822cf1e](https://github.com/Elympics/Unity-SDK/commit/822cf1e1b7bbedf82b6874927e9a3c31b10546e9))
* Replace tel-aviv region with dallas region ([8c3f3dd](https://github.com/Elympics/Unity-SDK/commit/8c3f3ddcef52695e33af2908c08d67624daea2f6))
* Retrieve Matchmaker and Game Engine data modified by EGB ([6c1bc3c](https://github.com/Elympics/Unity-SDK/commit/6c1bc3cffb829b8cb6e8af09a7c8a62a9dacb5e7))
* Return latency in addition to region name in ChooseClosestRegion method ([39f281f](https://github.com/Elympics/Unity-SDK/commit/39f281f78a4e14dc617f62c9e0f96a3403f0e862))
* Same name ElympicsVars in one ElympicsBehaviour error of the NSA collecting phase ([e084387](https://github.com/Elympics/Unity-SDK/commit/e084387a84c11b375d04a004d4bf8d70dd30f01a))
* Silence all errors and info printed while retrieving game version uploaded status ([cac7af4](https://github.com/Elympics/Unity-SDK/commit/cac7af4a8266fdb3d4f3f2eddbe723d542790adc))
* Use correct Elympics SDK version in web headers ([7715a78](https://github.com/Elympics/Unity-SDK/commit/7715a78e8493ee27ff4d08f94503293bec2f5f4d))
* Wait for server to fully initialize before marking it as initialized ([6c1d3e4](https://github.com/Elympics/Unity-SDK/commit/6c1d3e47064c0820c6fb2f3a01f457703e3fad12))
* webrtc client race condition on receive fixed, compatibility with 2019 unity ([c47765d](https://github.com/Elympics/Unity-SDK/commit/c47765daa966127665550183fa51846e7a1f88df))


### Features

* Add communication with leaderboards api ([f42a1a3](https://github.com/Elympics/Unity-SDK/commit/f42a1a359fafcfba902812514caa28caecc97dde))
* Allow authentication using Ethereum signature ([254ad16](https://github.com/Elympics/Unity-SDK/commit/254ad16c5521db9ebbc426c8fab7d2dd97607ed3))
* expose cancellation token in Play Online method ([51c2c60](https://github.com/Elympics/Unity-SDK/commit/51c2c608f834048377473849900ef40a7e7ec75d))
* Implement Networked Simulation Analyzer ([42c0043](https://github.com/Elympics/Unity-SDK/commit/42c00437185f3ee5473904b58a18f24c986e9b5d))
* include the player's input in the snapshot data. ([1cc4712](https://github.com/Elympics/Unity-SDK/commit/1cc471240315ae8702ccb4fdebeeda2a628838f4))
* Introduce SignOut method to ElympicsLobbyClient ([ff3bdd0](https://github.com/Elympics/Unity-SDK/commit/ff3bdd00d018b85765ef4df0e09214caef334eed))
* Introduce WebSocket-based communication with Elympics matchmaking ([8088651](https://github.com/Elympics/Unity-SDK/commit/808865125d51c78e866d27ca8aa7f6e41cc7a1ba))
* introduction to #LOG_TO_FILE preprocessor directive that allows writing Elympmics simulation data logs into the file ([9cdc0f4](https://github.com/Elympics/Unity-SDK/commit/9cdc0f4f2b70fcd63614da85dd8b8de4f164c587))
* new method allowing to play online in closest region ([b212572](https://github.com/Elympics/Unity-SDK/commit/b212572be7a3fce5e7d30a25b90fd0a8519ec6b4))
* provides base class for handle blockchain sending transaction and response ([a923373](https://github.com/Elympics/Unity-SDK/commit/a9233730906ae8470ca5b995fa5d68669b2935fc))



## [0.7.1](https://github.com/Elympics/Unity-SDK/compare/v0.7.0...v0.7.1) (2023-06-13)


### Bug Fixes 🪲

* Wait for server to fully initialize before marking it as initialized in the database ([65ce740](https://github.com/Elympics/Unity-SDK/commit/65ce740f3c1593db35552b360e51b5f536cbf5d7))



## [0.7.0](https://github.com/Elympics/Unity-SDK/compare/v0.6.3...v0.7.0) (2023-05-05)


### Features

* Add communication with Leaderboards API ([f42a1a3](https://github.com/Elympics/Unity-SDK/commit/f42a1a359fafcfba902812514caa28caecc97dde))
* Allow authentication using Ethereum signature ([254ad16](https://github.com/Elympics/Unity-SDK/commit/254ad16c5521db9ebbc426c8fab7d2dd97607ed3))
* Introduce SignOut method to ElympicsLobbyClient ([ff3bdd0](https://github.com/Elympics/Unity-SDK/commit/ff3bdd00d018b85765ef4df0e09214caef334eed))
* Add method allowing to play online in closest region ([b212572](https://github.com/Elympics/Unity-SDK/commit/b212572be7a3fce5e7d30a25b90fd0a8519ec6b4))



## [0.6.3](https://github.com/Elympics/Unity-SDK/compare/v0.6.2...v0.6.3) (2023-04-27)


### Bug Fixes 🪲

* Reload game config before starting game through ElympicsLobbyClient ([d3ae1b0](https://github.com/Elympics/Unity-SDK/commit/d3ae1b0975752e72f9b87d90fb3026ca3b5509a1))



## [0.6.2](https://github.com/Elympics/Unity-SDK/compare/v0.6.1...v0.6.2) (2023-04-24)


### Bug Fixes 🪲

* Fix OnSynchronized not being called on WebGL builds ([d447df4](https://github.com/Elympics/Unity-SDK/commit/d447df46452d44365347335ae462de00174d09f3))



## [0.6.1](https://github.com/Elympics/Unity-SDK/compare/v0.6.0...v0.6.1) (2023-04-18)


### Bug Fixes 🪲

* Fix IServerHandlerGuid methods not being called in ElympicsBehaviour ([eef5ff2](https://github.com/Elympics/Unity-SDK/commit/eef5ff2b77685c948eaf08e33394320dc4c4c473))



## [0.6.0](https://github.com/Elympics/Unity-SDK/compare/v0.5.4...v0.6.0) (2023-04-05)


### Features

* Introduce WebSocket-based communication with Elympics matchmaking ([8088651](https://github.com/Elympics/Unity-SDK/commit/808865125d51c78e866d27ca8aa7f6e41cc7a1ba))
* Provide base class for handling blockchain transactions ([a923373](https://github.com/Elympics/Unity-SDK/commit/a9233730906ae8470ca5b995fa5d68669b2935fc))



## [0.5.4](https://github.com/Elympics/Unity-SDK/compare/v0.5.3...v0.5.4) (2023-03-23)


### Bug Fixes 🪲

* Retrieve Matchmaker and Game Engine data modified by EGB ([6c1bc3c](https://github.com/Elympics/Unity-SDK/commit/6c1bc3cffb829b8cb6e8af09a7c8a62a9dacb5e7))



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
