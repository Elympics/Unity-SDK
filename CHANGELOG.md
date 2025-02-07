## [0.15.1](https://github.com/Elympics/Unity-SDK/compare/v0.15.0...v0.15.1) (2025-02-07)


### Bug Fixes 🪲

* grammar errors in tooltips ([aaa6576](https://github.com/Elympics/Unity-SDK/commit/aaa6576d9ccef8a48534ff2c905b002c0f8719f6))
* send RPC outside updateContex ([3bc17b9](https://github.com/Elympics/Unity-SDK/commit/3bc17b96154e4033a7dacbd3aa926bafeb4fbb5a))


### Features

* add jump threshold to game config ([69df921](https://github.com/Elympics/Unity-SDK/commit/69df9219c911cadb594480a1f53454df4106d072))
* add tool tips for configs ([f0892eb](https://github.com/Elympics/Unity-SDK/commit/f0892ebb302c00ebcd2647a300d6a1f526591dfa))
* improve warning message when prediction is blocked ([a994bec](https://github.com/Elympics/Unity-SDK/commit/a994becd56e83c23af8a3f6111fc6869040ab107))
* internal ElympicsLogger client ([d188f62](https://github.com/Elympics/Unity-SDK/commit/d188f623a5c6aebe53013bfbe7803bfadd126fda))



## [0.15.0](https://github.com/Elympics/Unity-SDK/compare/v0.14.3...v0.15.0) (2025-01-28)


### Bug Fixes 🪲

* throw exception when error during quickMatch ([2a0ebea](https://github.com/Elympics/Unity-SDK/commit/2a0ebea03dd60854d8b76185cbb5ed63d8af2134))


### Features

* introduction to previous input buffer ([a21ef62](https://github.com/Elympics/Unity-SDK/commit/a21ef62cc3ed2ede09d619313feb487cea268e8f))
* add missing ElympicsBehaviour states in server snapshot from local one ([36ed11c](https://github.com/Elympics/Unity-SDK/commit/36ed11ca850d3b6f1ea272fa2fceac32bd61eeae))



## [0.14.3](https://github.com/Elympics/Unity-SDK/compare/v0.14.2...v0.14.3) (2025-01-10)


### Features

* allow control over WebRTC through query parameters ([63d878e](https://github.com/Elympics/Unity-SDK/commit/63d878ee3a4760a693316cd07c4f36bd545af71f))
* expose ElympicsBehaviour.ElympicsBase ([171dc86](https://github.com/Elympics/Unity-SDK/commit/171dc86fa57bf3acba1f55eb74e60593267881a6))



## [0.14.2](https://github.com/Elympics/Unity-SDK/compare/v0.14.1...v0.14.2) (2024-11-13)

### Chore:
* refactor test abstraction for internal projects ([d14821](https://github.com/Elympics/Unity-SDK/commit/d14821c7f89982159cea87534b7efdbc239e9665))


## [0.14.1](https://github.com/Elympics/Unity-SDK/compare/v0.14.0...v0.14.1) (2024-11-06)


### Features

* add internal Elympics lobby state machine ([51801b7](https://github.com/Elympics/Unity-SDK/commit/51801b733e02cabd64548961089a6f67948ac801))
* add CustomRoomData and MatchmakingCustomData for quick match ([b5ab366](https://github.com/Elympics/Unity-SDK/commit/b5ab3662108725e8864857ed41ad35d295832e3f))
* refactor gameplay start/finish events ([4e5fd64](https://github.com/Elympics/Unity-SDK/commit/4e5fd6490dab7db58f05ce19804e5d7ffa17ead0))



## [0.14.0](https://github.com/Elympics/Unity-SDK/compare/v0.13.3...v0.14.0) (2024-10-04)

### BREAKING CHANGES

* Add mandatory initialization method for SCS ([5e3991](https://github.com/Elympics/Unity-SDK/commit/5e3991b77b8bae964ab503289cff2b08e9cd3d6c))


### Bug Fixes 🪲

* execution order from const ([4c3ca6f](https://github.com/Elympics/Unity-SDK/commit/4c3ca6f51d0360ddf0c8f99a28835f1eaf5c897a))
* refactor available region url creation ([a48b1c7](https://github.com/Elympics/Unity-SDK/commit/a48b1c726e6f82bc2a2d4df9815a260d3fd5ed6e))


### Features

* log uploaded version to the console ([d02a61f](https://github.com/Elympics/Unity-SDK/commit/d02a61f936e3485df709f0d652d6076ccd7a52be))



## [0.13.3](https://github.com/Elympics/Unity-SDK/compare/v0.13.2...v0.13.3) (2024-09-16)


### Hot Fixes 🪲

* add slash to uri ([469ef96](https://github.com/Elympics/Unity-SDK/commit/469ef96b9008db7964c2bd09523ef69674f608e3))



## [0.13.2](https://github.com/Elympics/Unity-SDK/compare/v0.13.1...v0.13.2) (2024-09-16)


### Bug Fixes 🪲

* update public room when user has left room ([503652f](https://github.com/Elympics/Unity-SDK/commit/503652fb5d8c9d7aef00564ccec631b7964e71c8))


### Features

* add available region check each time elympics connection is established ([b7c48b5](https://github.com/Elympics/Unity-SDK/commit/b7c48b5b379d123479b423e2a8217cfe735daa8f))



## [0.13.1](https://github.com/Elympics/Unity-SDK/compare/v0.13.0...v0.13.1) (2024-09-05)


### Bug Fixes 🪲

* Quick Match deadlock on exception ([f240ad6](https://github.com/Elympics/Unity-SDK/commit/f240ad6f71537342d790fb4c793421e71796418d))
* specific error logs on authFailed ([b51a1dd](https://github.com/Elympics/Unity-SDK/commit/b51a1dd33ff114bca499a278340428e0e080d0f4))



## [0.13.0](https://github.com/Elympics/Unity-SDK/compare/v0.12.0...v0.13.0) (2024-08-20)


### Bug Fixes 🪲

* add warning log for rpc call ([50dfb68](https://github.com/Elympics/Unity-SDK/commit/50dfb68757d6b6abb5f81695e28e71abedecd28f))


### Features

* refactor connect to elympics flow ([93358ef](https://github.com/Elympics/Unity-SDK/commit/93358ef88e6e66f8b758aa8a0f8d65820f0eccaf))



## [0.12.0](https://github.com/Elympics/Unity-SDK/compare/v0.11.0...v0.12.0) (2024-07-30)

### BREAKING CHANGES

* IWebSocketSession.Disconnected event signature change ([012819e](https://github.com/Elympics/Unity-SDK/commit/012819e59dca7677db1ef759596b7ffed451aca8))


### Bug Fixes 🪲

* add new webrtc library for macos ([c33ad2a](https://github.com/Elympics/Unity-SDK/commit/c33ad2a74de1513027e4e5aefdde2d7946d5f7ba))


### Features

* add web socket automatic disconnect detection ([cd99533](https://github.com/Elympics/Unity-SDK/commit/cd995331819fbf178352cd54eca22fe02adec6bf))



## [0.11.0](https://github.com/Elympics/Unity-SDK/compare/v0.10.0...v0.11.0) (2024-07-16)


### Bug Fixes 🪲

* add request for initial room status check ([3de0a28](https://github.com/Elympics/Unity-SDK/commit/3de0a286e7ef3cac4887d0cb2818c5719b2e6e75))
* left room event exception handling ([fecd4e7](https://github.com/Elympics/Unity-SDK/commit/fecd4e7d115ae2d22a33600fcb2007211b3ef382))


### Features

* add new sample: Elympics lobby interaction using room system ([047942b](https://github.com/Elympics/Unity-SDK/commit/047942baa129cfaacba815ad1a3c8d26c0e47cea))
* add telegram authentication type ([160a322](https://github.com/Elympics/Unity-SDK/commit/160a32227a0588dd65dddbe8fdc4db8146e14837))



## [0.10.0](https://github.com/Elympics/Unity-SDK/compare/v0.9.2...v0.10.0) (2024-06-12)


### Bug Fixes 🪲

* Add validation for new region ([4f20f53](https://github.com/Elympics/Unity-SDK/commit/4f20f53636b2c2bb48ce0f5f94b12b07288c469b))
* Don't call Application.Quit on server immediately after sending match results ([810aa46](https://github.com/Elympics/Unity-SDK/commit/810aa46d2bea8f845b3f0aae042aeb8ef98c00d9))
* Rename WebSocket-related JS methods to prevent conflicts ([fb0ca7d](https://github.com/Elympics/Unity-SDK/commit/fb0ca7dc3742e8751fb97b06457888f7fe5d8c64))
* Unsubscribe from event ([e12706e](https://github.com/Elympics/Unity-SDK/commit/e12706ef2c23fa08517c1b01f37d53db090f4067))


### Features

* Add async method for connecting with Elympics ([2b63656](https://github.com/Elympics/Unity-SDK/commit/2b63656b19fe18a8e68bf5439d36fbf523b7c755))
* Add initial region for Authentication ([a968be6](https://github.com/Elympics/Unity-SDK/commit/a968be69d98883729afda35e5050012f27ec7881))
* Add nickname to Authdata ([decae46](https://github.com/Elympics/Unity-SDK/commit/decae4644f97a142dc17eab5c009840734d45718))
* Add nickname to leaderboard response and room userinfo ([431ed4d](https://github.com/Elympics/Unity-SDK/commit/431ed4dbd3789a07fc63c60cba8423d540b2abda))
* Add respect service ([cc9b914](https://github.com/Elympics/Unity-SDK/commit/cc9b9149cb8e2bbdfbdd02e32fc3fb79eb79c873))
* Correct parsing optional in protobuf ([18b22d5](https://github.com/Elympics/Unity-SDK/commit/18b22d5cf092abaa146e13595f6f91c53f2e81b0))
* Extend initial match data model ([1ff5953](https://github.com/Elympics/Unity-SDK/commit/1ff5953cc940616e1d6450e6d801e906ffa34e13))
* HalfRemote and Local working with new IGameEngine interface ([45553c8](https://github.com/Elympics/Unity-SDK/commit/45553c84a7227a13784b0f64fe6409f667158ed0))
* Introduce Room System. ([78f4af5](https://github.com/Elympics/Unity-SDK/commit/78f4af53a369552d7fa326d3ee422e928c6d5ec4))
* Introduce Smart Contract Service ([5a59610](https://github.com/Elympics/Unity-SDK/commit/5a596102b4e37f2ce8556f792a7c7885b8a44d19))
* Make Elympics API addresses and client secret configurable through env variables ([a83244a](https://github.com/Elympics/Unity-SDK/commit/a83244a5f38723ad9b26df4fa12ff671a93c2519))
* Provide a mocking system for Rooms ([527fbca](https://github.com/Elympics/Unity-SDK/commit/527fbcade39d0f07f19fafc93f19998941345fa8))



## [0.9.2](https://github.com/Elympics/Unity-SDK/compare/v0.9.1...v0.9.2) (2023-09-25)


### Bug Fixes 🪲

* Fix game-breaking lags on Android by updating MessagePack to v2.5.124 ([983b782](https://github.com/Elympics/Unity-SDK/commit/983b7829e757984bb5ffb38717c36c0630eaea90))



## [0.9.1](https://github.com/Elympics/Unity-SDK/compare/v0.9.0...v0.9.1) (2023-09-08)


### Bug Fixes 🪲

* Fix RPC methods not being registered before calling IInitializable.Initialize ([babaab9](https://github.com/Elympics/Unity-SDK/commit/babaab9e3411fcf2b650d55901e63afa8c7c0d5d))
* Base URLs of lobby, auth and leaderboard service on general Elympics Cloud address ([968dcd6](https://github.com/Elympics/Unity-SDK/commit/968dcd6a8f05a56040a7718185b41c64ca7acea1))
* Retrieve unfinished matches only for the current version of the game ([37e3984](https://github.com/Elympics/Unity-SDK/commit/37e39843824e2fb8640925cec182924ae98bdbbd))
* Mark Elympics logs in a distinctive way, include timing, add missing logs ([e2190a6](https://github.com/Elympics/Unity-SDK/commit/e2190a661bde24de0b35664d7ce185549ffaa0ab))
* Provide verbose warning when there is not enough entries in Test players ([e2e2427](https://github.com/Elympics/Unity-SDK/commit/e2e2427697286098b6bd4e51b210c31b054926cd))
* Prevent Half Remote clients from starting if their ID is too high ([477dc50](https://github.com/Elympics/Unity-SDK/commit/477dc506afa636398f025b2d56eb6e66177bac5a))
* Update WebRTC DLLs to reduce delay while receiving data ([a0774a3](https://github.com/Elympics/Unity-SDK/commit/a0774a378a44eba41fecaaff4c8bea151ac896c2))
* Fix error handling in ElympicsCloudPing ([d1f4ad4](https://github.com/Elympics/Unity-SDK/commit/d1f4ad4ec42bce6c5e358aea59feb54a2d82d6dd))
* Prevent NRE from being thrown if client disconnects just after connecting to game server ([9d5b756](https://github.com/Elympics/Unity-SDK/commit/9d5b756ca7cbd14a25d84f331df4acf8ed99a8db))



## [0.9.0](https://github.com/Elympics/Unity-SDK/compare/v0.8.1...v0.9.0) (2023-08-28)


### Bug Fixes 🪲

* Reduce throttle warnings frequency ([779838e](https://github.com/Elympics/Unity-SDK/commit/779838ef7704e09fc6e3e4ad01bdf0cfcb31eb36))
* Prevent NRE when accessing current ElympicsGameConfig at startup ([c40cdb3](https://github.com/Elympics/Unity-SDK/commit/c40cdb3e67b48d6f91f4427a0dc598a0aba02923))


### Features

* Add ElympicsLobby to "Add GameObject" context menu ([624214b](https://github.com/Elympics/Unity-SDK/commit/624214b01031bc85fc75a20471165119d4f31fb0))
* Introduce RPC ([bcc9129](https://github.com/Elympics/Unity-SDK/commit/bcc9129621dc2e3dfa670210fd7dcbe2120787f6))



## [0.8.1](https://github.com/Elympics/Unity-SDK/compare/v0.8.0...v0.8.1) (2023-07-14)


### Features

* Integrate summing fetch type of leaderboard-service with the LeaderboardClient ([891bdda](https://github.com/Elympics/Unity-SDK/commit/891bdda22bbdeaa829280e88cd1d8e98e8d52f1e))
* Make rejoining last unfinished game possible ([fee0269](https://github.com/Elympics/Unity-SDK/commit/fee0269a28db737aaf707e9b3139dc4ddd831fc3))
* Send basic usage statistics to Elympics ([15eed80](https://github.com/Elympics/Unity-SDK/commit/15eed80a989f8f71d5f9d917a413fb43ce224f06))



## [0.8.0](https://github.com/Elympics/Unity-SDK/compare/v0.7.3...v0.8.0) (2023-07-05)


### BREAKING CHANGES :warning:

* Update minimal supported Unity version to 2021.3 (current legacy LTS) ([2585a1e](https://github.com/Elympics/Unity-SDK/commit/2585a1e9f9325bd1817f80723473b23ce7baca52))


### Performance Improvements 🚀

* Move ElympicsUpdate loop logic from FixedUpdate to Update ([47c831f](https://github.com/Elympics/Unity-SDK/commit/47c831f87c9f35360b0d96168ba87e36640cf591))


### Bug Fixes 🪲

* Fix regression causing region name not to be used when creating game server URL ([acfb18d](https://github.com/Elympics/Unity-SDK/commit/acfb18d1e7bf77d384f0f5f1e2dd7e3d6ae9f507))



## [0.7.3](https://github.com/Elympics/Unity-SDK/compare/v0.7.2...v0.7.3) (2023-06-29)


### Bug Fixes 🪲

* Fix the format of Ethereum address used in EthAddr authentication ([29deca5](https://github.com/Elympics/Unity-SDK/commit/29deca522ddddebefadedda68a0eabe24d293a48))
* Make CI-targeted build method throw on error ([719da0d](https://github.com/Elympics/Unity-SDK/commit/719da0dadccb79e6db1d78d37aac87a0a8bf78d8))
* Make real player count (retrieved from match data) available through ElympicsGameConfig.Players ([0ca518b](https://github.com/Elympics/Unity-SDK/commit/0ca518bef2e9df20e6b80898ecbaaa06385a9794))
* Mark implicit ElympicsVar cast as deprecated ([561ed5d](https://github.com/Elympics/Unity-SDK/commit/561ed5d33f5d42dbdf682e3d34b2abbd1dc88639))



## [0.7.2](https://github.com/Elympics/Unity-SDK/compare/v0.7.1...v0.7.2) (2023-06-22)


### Bug Fixes 🪲

* Choose closest region method did not returned valid region ([4db965f](https://github.com/Elympics/Unity-SDK/commit/4db965f911161bf08f387e0e69b838e9c0716634))


### Features

* New method returning latency for all available regions ([4db965f](https://github.com/Elympics/Unity-SDK/commit/4db965f911161bf08f387e0e69b838e9c0716634))



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
