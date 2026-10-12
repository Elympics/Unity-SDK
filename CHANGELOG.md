## [0.28.0](https://github.com/Elympics/Unity-SDK/compare/v0.27.0...v0.28.0) (2026-10-12)


### Bug Fixes 🪲

* Allow browsing Elympics(Game)Config when not logged in ([b9f4e7f](https://github.com/Elympics/Unity-SDK/commit/b9f4e7f31653eed5965ecffd577c007bb649d523))
* Apply MR suggestions ([89059eb](https://github.com/Elympics/Unity-SDK/commit/89059ebd29edab2901364d2cbd997f6755d4da8f))
* Call WeaveAssemblies less frequently ([c5d55f0](https://github.com/Elympics/Unity-SDK/commit/c5d55f01aa312a0ec2ca0c24a9b0b2012db175d9))
* Change default GS URL domain to elympics.ai ([22b4c4d](https://github.com/Elympics/Unity-SDK/commit/22b4c4db7342da6dab3c1236b29d71e1dbf5df1f))
* Clear disconnection CTS before canceling it ([2a4a96f](https://github.com/Elympics/Unity-SDK/commit/2a4a96f798d61466448452ab5b1ce7b27424c99b))
* Clear the screen to black in the generated boot scene ([d8d7de2](https://github.com/Elympics/Unity-SDK/commit/d8d7de2a105e7d3c0d0f9428d71071ee211e37a2))
* Copy errors to stderr only in server builds ([b431c22](https://github.com/Elympics/Unity-SDK/commit/b431c225829a5fab9e9a751c35598e0a2b797620))
* Correctly clear CancellationTokenSource-type fields in WebGameServerClient after disposing of them ([b769c84](https://github.com/Elympics/Unity-SDK/commit/b769c84ca4fad99f749b08421a7583ca89814b0f))
* Do not initialize a gameplay scene that activates during teardown ([b46867d](https://github.com/Elympics/Unity-SDK/commit/b46867db26c3dfb6e8fd5cf1028f7fe6c265898d))
* Don't end the game when the server handler's start wait is cancelled ([a4d3e13](https://github.com/Elympics/Unity-SDK/commit/a4d3e137048e59435f954f229cf8fead942e9d54))
* Don't tear down WebRTC connection on transient disconnected state ([0bf7d43](https://github.com/Elympics/Unity-SDK/commit/0bf7d43870c6b8ea6daf88471e087f825aa326b3))
* dontdestroyonload for root object ([9edb933](https://github.com/Elympics/Unity-SDK/commit/9edb933a47b6226a5de1275f7cadceb6019917a9))
* Extract rxSnapshotTicks into a separate class, fix bugs and provide tests ([610ec7f](https://github.com/Elympics/Unity-SDK/commit/610ec7f17ab6390295b999eed43821b3fc1bfaea))
* Fix a typo in jslib ([440af34](https://github.com/Elympics/Unity-SDK/commit/440af34c632764a83f57012b3b38974fb52bcfb7))
* Fix assembly stream being closed ([9ab041d](https://github.com/Elympics/Unity-SDK/commit/9ab041de2e78533376557f721878ef8b0e0a74f8))
* Fix bugs in Standalone player connection ([c8312fe](https://github.com/Elympics/Unity-SDK/commit/c8312fec15989f0052713297f6940cf0fa933acf))
* Fix bugs introduced after rebasing ([17cd9c4](https://github.com/Elympics/Unity-SDK/commit/17cd9c4f6ed27b3179edf94538f21c0fb44cb38f))
* Fix build error with assemblies referencing System.Private.CoreLib ([5734b3b](https://github.com/Elympics/Unity-SDK/commit/5734b3b2643cf4c1e87c0e622d27837daa91b656))
* fix Enable not enabling the ElympicsScore ([9c67247](https://github.com/Elympics/Unity-SDK/commit/9c6724796625b24ad8dc3380dc76d0d04a4243f5))
* Fix errors in weaved assemblies during build ([22f7605](https://github.com/Elympics/Unity-SDK/commit/22f760547385f5e93f1f410602cab5fd6a2225da))
* Fix Half Remote Client being stuck on Application.EnterPlayMode in Unity 2021 ([6f95e93](https://github.com/Elympics/Unity-SDK/commit/6f95e93f102dc5798f818d72f9cfc40619e03bff))
* Fix Half Remote Client not detecting WebRTC connection has been established ([500ef97](https://github.com/Elympics/Unity-SDK/commit/500ef97b4b873d3360c76d510cdfa4ceca9657b3))
* Fix JSON field case, include log level in JSON ([1c7a31e](https://github.com/Elympics/Unity-SDK/commit/1c7a31ea76f2793dec9a7cd67a52720a87e0d0d0))
* Fix missing log context in Debug Online mode ([961aff5](https://github.com/Elympics/Unity-SDK/commit/961aff5e3ed0056f66b461319428dcc34eb42120))
* Fix string buffers not being freed correctly in WebRtcCandidatePairChosenCallback ([41663e4](https://github.com/Elympics/Unity-SDK/commit/41663e429bebb4b352bd9dc7bf0a63d3142de64b))
* Fix this.unreliableOpened not being assigned in WebRTC JSLIB ([2330e8d](https://github.com/Elympics/Unity-SDK/commit/2330e8dcf333e5d7aa38e66425ca2d6358dd3d3b))
* Fix TURN not being detected for prflx-type candidates ([9adb2ea](https://github.com/Elympics/Unity-SDK/commit/9adb2eac55ede8fd9b9ab5dc295d23a0a3a330d2))
* Fix WebGL build by splitting into platform-specific assemblies and fixing .jslib ([70c5dbf](https://github.com/Elympics/Unity-SDK/commit/70c5dbf5aab23eb958530eb7a765906a6314c6d6))
* Fix WebRTC offer parsing in half remote ([131d0b7](https://github.com/Elympics/Unity-SDK/commit/131d0b7509330fd1dd468477966c6caee5cc20eb))
* force snapshot fix ([150c58f](https://github.com/Elympics/Unity-SDK/commit/150c58f2bec28d5fbd30a785dc80a83b0eba285b))
* Ignore EndGame calls after the game has ended ([0356fa8](https://github.com/Elympics/Unity-SDK/commit/0356fa8715d97c33c84573e0c8366fe81e3490dc))
* In project clones, link the Packages dir instead of copying it ([4abc60d](https://github.com/Elympics/Unity-SDK/commit/4abc60d8a6207fb55bf7beaafbc077bf80c00119))
* Initialize the replication pipeline in single player ([0c57feb](https://github.com/Elympics/Unity-SDK/commit/0c57febdfc802cac340b2b5ce894f0f063469309))
* Initialize the replication pipeline once on the half-remote server ([3041499](https://github.com/Elympics/Unity-SDK/commit/304149923a80aaf5fe990e92ec9e2932f802d3eb))
* Install missing meta files for Unity WebRTC package ([11861a8](https://github.com/Elympics/Unity-SDK/commit/11861a8d82e9abad3893156b7c1f8962f0937e49))
* Keep the Match Launcher tabs one line high and let the log fill the window ([46cf488](https://github.com/Elympics/Unity-SDK/commit/46cf488d2c2b87a7247f341569512bd558471c20))
* Let replay teardown release the replay controller's client ([34ed607](https://github.com/Elympics/Unity-SDK/commit/34ed607b7790c15b42efed276f66f70de9ca6401))
* Log half-remote connection retries as one-line warnings ([4c85a57](https://github.com/Elympics/Unity-SDK/commit/4c85a576959f81bbc5c722b920cfa7ddb282f355))
* Log snapshot collector disposal through ElympicsLogger ([9441d61](https://github.com/Elympics/Unity-SDK/commit/9441d6185e5911b10a2b889d9d31a11ee43e6661))
* Log the missing boot scene warning on the first return to boot ([f6d13a2](https://github.com/Elympics/Unity-SDK/commit/f6d13a233dedc52bb6372ed43df6474c7a206708))
* Lower default synchronizer timeout to 12s ([c224dee](https://github.com/Elympics/Unity-SDK/commit/c224dee14a0079724fff294d10372214dcaf1464))
* Make Debug Online mode working again ([9930716](https://github.com/Elympics/Unity-SDK/commit/99307161ed5c338ccfd04e012629c138afb1683b))
* Make half remote TCP connection non-blocking ([f3960a3](https://github.com/Elympics/Unity-SDK/commit/f3960a3633f047edec7146c767879837a69194ae))
* Make IClientHandlerGuid.OnDisconnectedByServer work correctly on half remote ([6385308](https://github.com/Elympics/Unity-SDK/commit/6385308a19fa7fe884fbde9934e618d2b385502a))
* Make IClientHandlerGuid.OnMatchEnded work correctly on half remote ([2a08806](https://github.com/Elympics/Unity-SDK/commit/2a0880683e93740389f004b6b10aa69f53fe7592))
* Make JSLIB return chosen candidates as strings ([d9d9452](https://github.com/Elympics/Unity-SDK/commit/d9d94525f31f276cdea2a8ab8c6fcdc537098abc))
* Make offer/answer/candidate JSON compatible with game server ([b89ecdd](https://github.com/Elympics/Unity-SDK/commit/b89ecdd219cd8ad1d3cf4b4b567576b1c4b63303))
* make rtt more responsive ([43360c9](https://github.com/Elympics/Unity-SDK/commit/43360c984b3affb2dcc2d733c6882410c18ab570))
* Make score timestamp be in milliseconds instead of seconds ([1c8b0d3](https://github.com/Elympics/Unity-SDK/commit/1c8b0d39965ef7cc6c05a73038a31a3e705a4fbd))
* Move ElympicsGetHref jslib from PlayPad and implement it correctly ([dae9f53](https://github.com/Elympics/Unity-SDK/commit/dae9f53d16dc247bd579426336bf993eacbda71a))
* Pass synchronization data to RoundTripTimeCalculator on every synchronization ([b130c5d](https://github.com/Elympics/Unity-SDK/commit/b130c5d5366e81b66a2e9b69d241d86d11014654))
* Prevent dropped requests from crashing SimpliHttpSignalingServer ([42d41a0](https://github.com/Elympics/Unity-SDK/commit/42d41a0673b1a7e61fbaf20eb0d7ad988fcb380b))
* Prevent ElympicsLobbyClient from creating its own instance of logger context, remove redundant prefix from webrtc.jslib logs ([8c2188f](https://github.com/Elympics/Unity-SDK/commit/8c2188f8c85541898365d63e5c2822dfdfaa7718))
* Prevent fails in CI when quitting batchmode ([06cd43e](https://github.com/Elympics/Unity-SDK/commit/06cd43e7d0da6654dbdc4f54134f6266de5fdfbf))
* Read replays whose snapshots contain unchanged states ([41ac658](https://github.com/Elympics/Unity-SDK/commit/41ac658c10e2bddbdc96e7f4268c20294c647d79))
* Record each snapshot with its own tick end time ([f372fa9](https://github.com/Elympics/Unity-SDK/commit/f372fa9b1ebc2dd868deb5e67379f7ba911d97a6))
* Refresh an expired developer token when entering Play mode ([4dc694e](https://github.com/Elympics/Unity-SDK/commit/4dc694ed4aea5946eff23010205536a1c3bf05b0))
* Re-initialize ClientSynchronizer on every reconnect ([1ea0c09](https://github.com/Elympics/Unity-SDK/commit/1ea0c09cb9439b223f50cfccc820bb5f75e288ca))
* Reject TCP/UDP launches on WebGL ([36bd767](https://github.com/Elympics/Unity-SDK/commit/36bd76759063af688ae8a11cb79e4592632dd76e))
* Release the physics simulator's static instance on destroy ([3feeda3](https://github.com/Elympics/Unity-SDK/commit/3feeda3445e10a63bf24ca304f0c7ca719b08f14))
* Remove DateTime.UtcNow call from GetGameplayTimeDelegate ([eae9405](https://github.com/Elympics/Unity-SDK/commit/eae9405937ddb434d64dc3865365a44cc9d84812))
* Report a launch or spawn exception once instead of logging it before throwing ([da6c16b](https://github.com/Elympics/Unity-SDK/commit/da6c16b9777586f9326a3ed7a7d7d40f5e8b71dc))
* Reset last received snapshot counter in ElympicsWorld.ActivatePlayer ([4873e89](https://github.com/Elympics/Unity-SDK/commit/4873e89ba8aa05f96e6282aee55ff7d5be33761a))
* Restore the open scenes after a Match Launcher launch from edit mode ([2242ef7](https://github.com/Elympics/Unity-SDK/commit/2242ef78a0e9094361c1f137a9bfcae2b88553a9))
* Return to a generated empty scene when the boot scene is unusable ([2496296](https://github.com/Elympics/Unity-SDK/commit/24962969ef3528f2d541a30c1c1ff41112c21c11))
* Size Match Launcher event lines to their wrapped text ([bf95063](https://github.com/Elympics/Unity-SDK/commit/bf950635a8584e22d2b6f8218efb4b29805ee939))
* Size the Match Launcher tabs by their labels ([940afe3](https://github.com/Elympics/Unity-SDK/commit/940afe3c0c172d91e360e5bd38334f38bd396d96))


### Features

* Add context menu entry for creating Weaver settings SO ([14457e5](https://github.com/Elympics/Unity-SDK/commit/14457e52d3a3d166e315c5c3fd73e87d0688c981))
* Add context to WebRTC logs ([875bbb3](https://github.com/Elympics/Unity-SDK/commit/875bbb38ad0a2fc008308be17f01ac1e4ec0b209))
* add custom date for score report ([e7c3ad9](https://github.com/Elympics/Unity-SDK/commit/e7c3ad94099ce563e02c09dbab9b32e44a5b0e64))
* Add event on chosing WebRTC candidate pair, include UsesTurn in structured logs ([117f4b9](https://github.com/Elympics/Unity-SDK/commit/117f4b92d5906299f1a8edf699915ba227c09331))
* Add host-driven ElympicsMatchLauncher ([54b5888](https://github.com/Elympics/Unity-SDK/commit/54b588850d48ea29982a87fbc5bc96b32190d424))
* Add 'Manage games...' section for uploading StreamingAssets bundles ([7abd46f](https://github.com/Elympics/Unity-SDK/commit/7abd46fda36e5a59328cffca53d24393ae0ef75f))
* Add match launch request protocol types ([9136a90](https://github.com/Elympics/Unity-SDK/commit/9136a900294341e07fda0399ed93b2801790041f))
* Add match launcher build validation and Editor window ([947f81d](https://github.com/Elympics/Unity-SDK/commit/947f81d9d24a1d0eafe210e3088168456b622faf))
* Add panel for choosing Elympics DEFINE symbols used ([e940214](https://github.com/Elympics/Unity-SDK/commit/e940214c57b07ba0553e630ab9506f80a43a0458))
* Add WebGL and native host bridges for the match launcher ([2b17da7](https://github.com/Elympics/Unity-SDK/commit/2b17da7853adfd9700026a52080a42099305ed5a))
* Assign Network IDs in automated way only ([4285dec](https://github.com/Elympics/Unity-SDK/commit/4285dec8386ac2a6196c088e313b889da0966a70))
* Configure half-remote matches from launch data ([2527767](https://github.com/Elympics/Unity-SDK/commit/252776785ecb3de4baff93fd347a02cf0e0f6f70))
* Expose internals to the PlayPad lobby assemblies ([4963728](https://github.com/Elympics/Unity-SDK/commit/4963728197000728fd10cb4264b06a27283a4da9))
* Implement outlets for outputting logs in different formats ([3bd38b8](https://github.com/Elympics/Unity-SDK/commit/3bd38b86fc7521a95a6900dcde057068c3ea7a84))
* Include network details in JSON breadcrumbs ([a5f1c1b](https://github.com/Elympics/Unity-SDK/commit/a5f1c1bb24ab6d2b47bb86f244b56517447a91a2))
* Introduce a structure for informing PlayPad package about lost packets ([72dcd0d](https://github.com/Elympics/Unity-SDK/commit/72dcd0df237ac7deded1246c5d00542c0624a7ba))
* Introduce intermediate score submission ([2958b72](https://github.com/Elympics/Unity-SDK/commit/2958b724b9f8a2e081baf9db8156d207f06fd886))
* Launch a snapshot replay from a loaded retriever ([b0d7a46](https://github.com/Elympics/Unity-SDK/commit/b0d7a468ff909ffabc95ecea6ec34bdec2599f27))
* Launch from the Match Launcher window with its own data, in UI Toolkit ([8929ba0](https://github.com/Elympics/Unity-SDK/commit/8929ba0f54326e572e9c60c41d258233b86561e7))
* Launch matches with typed per-mode launch data ([7078795](https://github.com/Elympics/Unity-SDK/commit/7078795212dac61b74415e9bc02233c975dc7e83))
* Launch snapshot replays from a path or URL ([7f4fffa](https://github.com/Elympics/Unity-SDK/commit/7f4fffa84c04f0e6535680afa7f023a9f5aa1106))
* Leave server and half-remote code out of WebGL players ([2264e36](https://github.com/Elympics/Unity-SDK/commit/2264e36092dc06088fef10cddf7e7d45675365a4))
* Log every emitted host event at debug level ([7c6a8cb](https://github.com/Elympics/Unity-SDK/commit/7c6a8cbb899e1ce6e61f2829d87de855ad31285f))
* Make debug network logs always be sent to server with ELYMPICS_DEBUG define present ([ff098ff](https://github.com/Elympics/Unity-SDK/commit/ff098ff34b0584b4f509a2db241f81fe8b4ed6d4))
* Make repeated match launches release their resources ([bf558a9](https://github.com/Elympics/Unity-SDK/commit/bf558a9426da39a62dd2921becfd527b702ab4be))
* Per-mode launch commands on host bridges ([68f1eb4](https://github.com/Elympics/Unity-SDK/commit/68f1eb42c7888c275fbed24700aa07f5904b3536))
* Pick the Match Launcher's game from a drop-down ([bca3583](https://github.com/Elympics/Unity-SDK/commit/bca35833bf13010a4a52705546b76653dce605ff))
* Pick the Match Launcher's launch mode with tabs ([8070509](https://github.com/Elympics/Unity-SDK/commit/8070509b6711a4ce934eca5ed0c159ad9ee76a16))
* Provide an explicit Seed getter in the server InitialMatchPlayerData list ([50cd230](https://github.com/Elympics/Unity-SDK/commit/50cd230e99bf4a7976354bfb588c8b919a2b944e))
* Remove gameplay-scene debug modes in favour of the Match Launcher window ([2832fce](https://github.com/Elympics/Unity-SDK/commit/2832fcefdce937df1e1bee39d0ed66904198f416))
* Restore the lobby, auth and client secret launch parameters ([8defc0c](https://github.com/Elympics/Unity-SDK/commit/8defc0c80c1f7353df13a9afd137f423029a8f0c))
* Start a match from ELYMPICS_LAUNCH_MODE ([76ef753](https://github.com/Elympics/Unity-SDK/commit/76ef7534a258c63fb975ef8c9322d2a717c5eb8e))
* Support bots in SinglePlayer mode ([bc6a3cf](https://github.com/Elympics/Unity-SDK/commit/bc6a3cf7f6b4e1c2aad46f503c18dfa59c618c9e))
* Throw GameServerClosedException in case of HTTP 502 error ([8895219](https://github.com/Elympics/Unity-SDK/commit/889521998fead50128df0af7b511411c10747cb8))
* Warn about Newtonsoft.Json in runtime code ([e43824f](https://github.com/Elympics/Unity-SDK/commit/e43824f12f64a2c56292cef70b90db43d6bfebbd))


### Code Refactoring

* Make ElympicsMatchLauncher an abstract class only the SDK implements ([529d11b](https://github.com/Elympics/Unity-SDK/commit/529d11baa3a9019553807086f35e87c93c67b8f3))
* Remove orphaned lobby-era types and dead parameters ([8d3b3e8](https://github.com/Elympics/Unity-SDK/commit/8d3b3e8fd11cf360bf9cae6e8d3ffd246ab2e45a))
* Rename AbortMatchAsync to ReturnToBootAsync ([6d99f8e](https://github.com/Elympics/Unity-SDK/commit/6d99f8ec4c52ffc6ffb626fb1bdee257c2ba3aad))
* Replace AsyncEventsDispatcher with UniTask main-thread posting ([5f2088e](https://github.com/Elympics/Unity-SDK/commit/5f2088efb1515a022850a58740d6f4928d9769ee))
* Simplify match launcher and remove obsolete leftovers ([7718732](https://github.com/Elympics/Unity-SDK/commit/7718732ecd68cad194909d139f2a3403de8007ed))


### Performance Improvements 🚀

* Force garbage collection after a match only when verifying teardown ([ea45601](https://github.com/Elympics/Unity-SDK/commit/ea4560152837f3fea5e16b6a4ca166e6bd218d8c))
* Stop polling config and Build Settings in the Match Launcher window ([0a6e4d2](https://github.com/Elympics/Unity-SDK/commit/0a6e4d25e3af64a74f71a54d554fcb042d9d68f6))



### BREAKING CHANGES :warning:

* **Launching matches.** Matches start through `ElympicsMatchLauncher.Instance` with typed per-mode launch data: `LaunchOnlineAsync`, `LaunchSinglePlayerAsync`, `LaunchHalfRemoteClientAsync`, `LaunchHalfRemoteServerAsync` and `LaunchSnapshotReplayAsync`. Return to the boot scene with `ReturnToBootAsync` and track progress with `State`, `IsCurrentlyInMatch`, `StateChanged`, `MatchEnded` and `Disconnected`. `ElympicsMatchLauncher` is an abstract class only the SDK implements (Castle DynamicProxy mocks still work). `IElympicsMatchLauncher`, `IMatchLauncher`, `ILobby`, `LobbyRegister`, `IGameplaySceneMonitor`, `ElympicsLaunchRequest`, `MatchmakingFinishedData`, `JoinedMatchMode` and `ElympicsLaunchErrorCode.ProtocolMismatch` are removed. `ElympicsLaunchDefaults.NoPostMatchTimeout` is renamed to `NoTimeout`.
* **Removed launch paths.** Pressing Play in a gameplay scene no longer starts a match. Use **Tools > Elympics > Match Launcher**, or set `ELYMPICS_LAUNCH_MODE` / `--elympics-launch-mode`. `UNITY_SERVER` builds need `--elympics-launch-mode=halfRemoteServer` or a host launch. ParrelSync clones get `--elympics-launch-mode=halfRemoteClient --half-remote-player-index=<N>` as their default arguments.
* **Boot scene.** Set `ElympicsConfig > Boot scene` and add it and every gameplay scene to Build Settings. Without a boot scene, matches return to a generated empty scene.
* **Host protocol 3.0.0 / native API 3.** There is one launch command per mode. `returnToBoot` replaces `abortMatch`, and `returnToBootAccepted`/`returnToBootFailed` replace `abortAccepted`/`abortFailed`. `ElympicsHost_LaunchMatch`, `ElympicsHost_AbortMatch`, `window.elympicsHost.launchMatch` and `window.elympicsLaunchData` are removed: pages call a launch method after the `ready` event. Native hosts must be rebuilt. See `Documentation~/MatchLauncher.md`.
* **WebGL players** leave out server and half-remote server code. The half-remote client needs the `ELYMPICS_WEBGL_HALF_REMOTE` scripting define.
* **Config.** `ElympicsConfig.currentGame` is removed: the active game is the first entry of `availableGames` (reorder games in Manage Games to pick another). The gameplay-scene debug modes are removed, together with `GameplaySceneDebugModeEnum`, `HalfRemoteModeEnum`, `GetHalfRemoteMode`, `GetHalfRemotePlayerIndex` and `IsOverridenInHalfRemoteByClone`. `LocalPlayerAndBots` is removed: SinglePlayer supports bots.
* **Removed features:** Rooms, the lobby client (`ElympicsLobbyClient`), the auth service client, the legacy Matchmaker and the Debug Online mode, Respect, Leaderboards, SmartContractService, Web3/TON signers, the standalone bot build, the Tech Demo and Simple Menu samples, and the Guid-less `IClientHandler`/`IServerHandler`/`IBotHandler` (use the `I*HandlerGuid` interfaces). The lobby, auth and Rooms code moved to the PlayPad SDK (3.0.0).
* **ElympicsScore** holds one value per player: index it with `score[playerIndex]`. It no longer enumerates per-player arrays.
* `AsyncEventsDispatcher` and `IAsyncEventsDispatcher` are removed. Scenes that added the component outside the Elympics prefab show a missing script: remove it.
* Also removed: `ElympicsClient.TimeSynchronized` (RTT is published through `CrossAssemblyEventBroadcaster`), `StringBytesConverter`, `ITelegramSigner`, `RegionData` and `MatchDetails`.
* `IReplayManipulator` has a new `UnloadReplay` method that custom implementations must add.
* Analyzer rule `ELY001` (server-to-players RPC in `IInitializable.Initialize`) is renamed to `ELW001`. Update its suppressions.



## [0.27.0](https://github.com/Elympics/Unity-SDK/compare/v0.26.0...v0.27.0) (2026-04-16)


### Bug Fixes 🪲

* Add Burst 1.8.3 dependency to avoid issues with Mono.Cecil ([859e7f4](https://github.com/Elympics/Unity-SDK/commit/859e7f4b11c9d12b81146f59d1c9000e5e6aeb95))
* Add Rider IDE dependency ([e72f9e7](https://github.com/Elympics/Unity-SDK/commit/e72f9e756f2ccf644b08a44f078d4af578142e84))
* Correctly assign Sender in local mode and tests ([ba2b5e7](https://github.com/Elympics/Unity-SDK/commit/ba2b5e71151340a84132598ef03293cf690be720))
* Don't mark scene dirty if network ID wouldn't change ([a9eb6ca](https://github.com/Elympics/Unity-SDK/commit/a9eb6caa7b6083f2843cf6192ef33e3935dc48ae))
* Fix NRE when checking for authentication status with no PlayPadLobby on scene ([fc86d33](https://github.com/Elympics/Unity-SDK/commit/fc86d33c15746e6e3267d9bde606b0814dcda306))
* Fix parent private RPCs from base classes not being called ([9bc1832](https://github.com/Elympics/Unity-SDK/commit/9bc1832250f97f7820085710ef24a1d4591c64ac))
* Make network IDs auto-reassign on every save, play and load ([584684d](https://github.com/Elympics/Unity-SDK/commit/584684dc90a2d0ceacb85e236f795febdacbe228))
* Recompile DLLs when Weaver is removed ([48f839f](https://github.com/Elympics/Unity-SDK/commit/48f839fedb5829e1dd56b7a591abfdb2ec945940))
* Remove references to Mono.Cecil from runtime Elympics assembly ([afb5179](https://github.com/Elympics/Unity-SDK/commit/afb51793f49aa70d4588427fa74d550c3f0412f8))
* Update dependencies to include TextMeshPro ([8a3fbcf](https://github.com/Elympics/Unity-SDK/commit/8a3fbcf6217f1afc2e2e7bc79ca80ebec31b7000))


### Features

* Add Roslyn analyzer for warning when server-to-player RPC is used in Initialize ([96261cf](https://github.com/Elympics/Unity-SDK/commit/96261cf237967dcc0e4445057fafc1146f19951e))
* Add WaitForTick property to ElympicsRpc attribute ([d1e9dff](https://github.com/Elympics/Unity-SDK/commit/d1e9dffbda646e88fda21c2c94c9969756b380d5))
* Allow for sending RPCs over unreliable channel ([368220b](https://github.com/Elympics/Unity-SDK/commit/368220be3feb2503398182d10515cf7e3b63bd27))
* Allow RPCs to be called in all contexts ([a55db03](https://github.com/Elympics/Unity-SDK/commit/a55db03c2d9ff81bab3c4d49595b5551d04ec3bc))
* block rpc messages. ([8947f89](https://github.com/Elympics/Unity-SDK/commit/8947f890f28ba4fd46f60de56aaf7b86e91faa66))
* Introduce RPC metadata with information on RPC sender ([9eff1cd](https://github.com/Elympics/Unity-SDK/commit/9eff1cd3a35edaafc38689c065a87905af567be1))
* Make assemblies recompile when scripts in Elympics(.Editor).Weaving change ([9e89cea](https://github.com/Elympics/Unity-SDK/commit/9e89ceaafa72c090c1e24ff91f1c2439892d9324))
* Mark start and end of injected IL code ([fa7a579](https://github.com/Elympics/Unity-SDK/commit/fa7a579436ba16d8ffc76964c97fe5592cf90d33))
* Put Version in [ProcessedByElympics] ([bb6adc3](https://github.com/Elympics/Unity-SDK/commit/bb6adc321327b824c06a46f613d6decf030445cb))
* Webrtc ice servers support ([954eda9](https://github.com/Elympics/Unity-SDK/commit/954eda9a7064ce9599d6bb01b2a21cdc94ef1f35))



## [0.26.0](https://github.com/Elympics/Unity-SDK/compare/v0.25.1...v0.26.0) (2026-03-24)


### Bug Fixes 🪲

* Fix the issue with Elympics.dll becoming locked ([2db404d](https://github.com/Elympics/Unity-SDK/commit/2db404dc6f786b0647181e8beeddac3a16a34132))
* Provide additional countermeasures to Elympics.dll locking problem ([b53ab90](https://github.com/Elympics/Unity-SDK/commit/b53ab907372b05f6cabf686952b5aae1d3e9761a))
* Run weaving from a static class, not depending on a ScriptableObject ([4843722](https://github.com/Elympics/Unity-SDK/commit/48437227b2f734efef7af4af1dc9860dcd0f7c78))


### Features

* Add conditional ElympicsLogger.LogDebug for logging only when ELYMPICS_DEBUG is defined ([26dd92b](https://github.com/Elympics/Unity-SDK/commit/26dd92bea8a2d845bf9c99e3e0d34d8620b3a901))
* Internal world replica state introduction ([b2e4e8f](https://github.com/Elympics/Unity-SDK/commit/b2e4e8f5acee443009b676f18c86ce96a357b4a9))



## [0.25.1](https://github.com/Elympics/Unity-SDK/compare/v0.25.0...v0.25.1) (2026-03-23)


### Bug Fixes 🪲

* Ignore unknown chain types ([7a4a58b](https://github.com/Elympics/Unity-SDK/commit/7a4a58b6744cc53e2c895cfdf8c818cb2d1f6f51))



## [0.25.0](https://github.com/Elympics/Unity-SDK/compare/v0.24.3...v0.25.0) (2026-02-25)


### Bug Fixes 🪲

* add missing dependencies on packages and modules ([37f4c99](https://github.com/Elympics/Unity-SDK/commit/37f4c991f6dde80ad0556fffbe42d840cb6a60fd))
* batchmode client upload synchronous ([2b4eadc](https://github.com/Elympics/Unity-SDK/commit/2b4eadcefa618ef359012555d5655c99c8ae4b51))
* ElympicsVector2 not using tolerance correctly ([ae03c32](https://github.com/Elympics/Unity-SDK/commit/ae03c32e0b06663bf02ec63f5aa7dfde87ccae79))
* ElympicsVector3 not using tolerance correctly ([38044eb](https://github.com/Elympics/Unity-SDK/commit/38044eb3698d8c36140b154b3dce52287c3887dc))
* invalid operation exception ([0f76658](https://github.com/Elympics/Unity-SDK/commit/0f76658360b49ca291cdd0859606da0657b9dad8))
* networkid sync fix ([84f01c4](https://github.com/Elympics/Unity-SDK/commit/84f01c4892453c8f50ae15c93cd09b29ade30a4d))
* prediction warnings showing incorrect values ([3d37086](https://github.com/Elympics/Unity-SDK/commit/3d3708637239769cfd0f14a110857e40714992d5))
* rigidbody synchronization on disabled objects ([0d3b209](https://github.com/Elympics/Unity-SDK/commit/0d3b2097a856f9a76649e6390395c05f2cea4147))


### Features

* BREAKING CHANGE: add generative network ids ([7508a75](https://github.com/Elympics/Unity-SDK/commit/7508a75c234dd91fa2cce76d99141b73cfd9ed04))



## [0.24.3](https://github.com/Elympics/Unity-SDK/compare/v0.24.2...v0.24.3) (2026-01-27)


### Bug Fixes 🪲

* add content type and encoding headers ([cd3f790](https://github.com/Elympics/Unity-SDK/commit/cd3f790b2fabc34fbb95af548263d8a715e0329b))


### Features

* scroll view for manage games editor window ([0d7a0cc](https://github.com/Elympics/Unity-SDK/commit/0d7a0cc8840c5318d563d7e0a24f1d088fa37c0c))



## [0.24.2](https://github.com/Elympics/Unity-SDK/compare/v0.24.1...v0.24.2) (2026-01-23)


### Bug Fixes 🪲

* detect compressed builds to send ([a5c4a10](https://github.com/Elympics/Unity-SDK/commit/a5c4a1034d4c9a5687842816ef7fd4400c8d6ff3))



## [0.24.1](https://github.com/Elympics/Unity-SDK/compare/v0.24.0...v0.24.1) (2026-01-22)


### Bug Fixes 🪲

* exception when preparing a snapshot to be sent on server ([d79fd07](https://github.com/Elympics/Unity-SDK/commit/d79fd079a33bb65ff6ae3d42ef5e666288eeba26))



## [0.24.0](https://github.com/Elympics/Unity-SDK/compare/v0.23.0...v0.24.0) (2026-01-21)


### Bug Fixes 🪲

* allow setting input lag in game config to up to 1 second instead of just 15 ticks ([b2c53ea](https://github.com/Elympics/Unity-SDK/commit/b2c53eabb43bb36e70b794b6fb1ff1c3ff0211dc))
* remove data that is not part of object state from snapshot state byte size calculation ([e49f166](https://github.com/Elympics/Unity-SDK/commit/e49f166c6321d14d0c0571cd7c7cd152c6a4b499))


### Features

* internal: add wordlSnapshotState ([3b7507c](https://github.com/Elympics/Unity-SDK/commit/3b7507cde371895eff981781fe66d2aa958c38d9))
* remove the ability to disable prediction globally from game config ([28a34d2](https://github.com/Elympics/Unity-SDK/commit/28a34d2ebe6183de6940745f373cb364780bd144))
* uploading webgl client builds on platform ([b9a5463](https://github.com/Elympics/Unity-SDK/commit/b9a546387b41568fbb8899551be8e9c24443ad84))



## [0.23.0](https://github.com/Elympics/Unity-SDK/compare/v0.22.0...v0.23.0) (2026-01-13)


### Bug Fixes 🪲

* add com.unity.nuget.newtonsoft-json to Elympics SDK dependencies ([271d354](https://github.com/Elympics/Unity-SDK/commit/271d354f8947b2b10119500f7eb27cab2a8fd80a))
* add fix to jslib to support firefox ([aa0fee5](https://github.com/Elympics/Unity-SDK/commit/aa0fee57446cf2aae716f9ca6291fe947109c746))
* catch UnityWeb exception ([38220fa](https://github.com/Elympics/Unity-SDK/commit/38220fa29f504580777dff3e8e7e4e5f057757c6))
* change SerializableField to Serializable attribute ([af11471](https://github.com/Elympics/Unity-SDK/commit/af11471f0f38dd846767b6f1006bd593029da597))
* enable synchronization for ElympicsVars set directly in the Unity inspector ([c8370a6](https://github.com/Elympics/Unity-SDK/commit/c8370a6d731c217304b51f60fadb6f62ef16cb51))
* invoke connectedCallback in HalfRemoteMatchConnectClient.ConnectAndJoinAsPlayer ([a032155](https://github.com/Elympics/Unity-SDK/commit/a0321550cd3e997c96e943c3237f7c7c10b1aa69))
* properly resolve type name when WaitForLobbyData operation times out ([09f7e41](https://github.com/Elympics/Unity-SDK/commit/09f7e41a0da8b5b34dc0a21404b873b151603791))
* remove thread sleep during WebRtc connection establishment ([9ce880d](https://github.com/Elympics/Unity-SDK/commit/9ce880d2a70002d89b12e069c5bcf823495191e0))


### Features

* add a summary for the ElympicsVar<T>.ValueChanged event ([c6bf694](https://github.com/Elympics/Unity-SDK/commit/c6bf6940c0a5a40b28ff26e15cefef6b38fdef0c))
* add custom property drawer for basic ElympicsVar types ([a480a57](https://github.com/Elympics/Unity-SDK/commit/a480a57975212c398ba1417c5ee29a7db0dd1931))
* add custom property drawers for ElympicsArray and ElympicsList ([73f8af7](https://github.com/Elympics/Unity-SDK/commit/73f8af71c97fe48afef45f127cfb2f3efdd9f258))
* add custom warnings for different cases of object instantiation and destruction prediction errors ([3e7c912](https://github.com/Elympics/Unity-SDK/commit/3e7c91207016bc450d4f4a938436aaf07b06fe2d))
* add ElympicsLong type of ElympicsVar ([dfceaa0](https://github.com/Elympics/Unity-SDK/commit/dfceaa063dd05fe1abf37325c2b4e0ef2a54da3c))
* add GameObject name and network ID to warnings logged for fields with incorrectly predicted state on client ([cb36bb6](https://github.com/Elympics/Unity-SDK/commit/cb36bb67a7fb19315961e3399375e0413cc00960))
* add missing match initial data on server ([1e4bf30](https://github.com/Elympics/Unity-SDK/commit/1e4bf300bbdf6be330348891c9548330ebdc40c4))
* add optional ELYMPICS_PRODUCTION symbol that can be defined to improve performance by removing logs in some cases ([57b6e91](https://github.com/Elympics/Unity-SDK/commit/57b6e9145cbf76be4e467910878ed4a055a13b9d))
* add optional position and rotation to the ElympicsMonoBehaviour.ElympicsInstantiate method ([e01bc76](https://github.com/Elympics/Unity-SDK/commit/e01bc76484a34b7b2548fd1fecd266ab10c664ab))
* add prediction status information handler ([61f255f](https://github.com/Elympics/Unity-SDK/commit/61f255f32418c070c1cc7f9ef581b0a5bc78a6da))
* add specific warnings for when entire factory state is incorrectly predicted to exist or not exist on client ([6408932](https://github.com/Elympics/Unity-SDK/commit/6408932637570f3294c7b12d4c02f9d863528e89))
* add tick numbers to prediction warnings ([786ac31](https://github.com/Elympics/Unity-SDK/commit/786ac3192fd5955e44a47b7f83847d4eadbc0974))
* allow enabling and disabling ElympicsVar synchronization in inspector for ElympicsVars stores in Unity serialized fields ([05b09b7](https://github.com/Elympics/Unity-SDK/commit/05b09b79175b90729a53449ef4e70970a7151cc7))
* improve error message for incorrect use of DestroyInstance, so it explains that the ArgumentException can be caused by calling that method on the same object more than once ([c8420bd](https://github.com/Elympics/Unity-SDK/commit/c8420bda4a8e8c1a735c2bacff1035de98f868f2))
* improve the clarity of the warning logged when client doesn't predict spawning or destruction of objects correctly ([ca4b018](https://github.com/Elympics/Unity-SDK/commit/ca4b01817d5cf0ebb7bd31d53eb810d6d2d96e04))
* increase default prediction limit ([f9638e2](https://github.com/Elympics/Unity-SDK/commit/f9638e2862753907efbf5fb35f1829174d166a55))
* make factory methods optional in constructors of ElympicsArray and ElympicsList ([f928d5c](https://github.com/Elympics/Unity-SDK/commit/f928d5c12f1ae046ec3159f76a5c3c374fd17d58))
* remove redundant warning logged once per behaviour when any of its fields have incorrectly predicted state on the client ([9c783d8](https://github.com/Elympics/Unity-SDK/commit/9c783d8e7830f0884518f4d1c4d9099411b5d994))
* send match initial data to clients ([2d3f231](https://github.com/Elympics/Unity-SDK/commit/2d3f23110cad9da921a40aeca9c58e639d87a1a6))



## [0.22.0](https://github.com/Elympics/Unity-SDK/compare/v0.21.1...v0.22.0) (2025-11-05)

### Bug Fixes 🪲

* stopping client synchronizer ([e21e81f](https://github.com/Elympics/Unity-SDK/commit/e21e81fd9d203281b67ee55e368aa001aa76a59f))

## [0.21.1](https://github.com/Elympics/Unity-SDK/compare/v0.21.0...v0.21.1) (2025-10-15)


### Bug Fixes 🪲

* suppress the "Operation WaitForResult timed out" error log when the WebSocketSession.SendRequest method throws an exception ([1d38182](https://github.com/Elympics/Unity-SDK/commit/1d38182781859a605ecde0e30470823b2b791930))
* throw an exception with a useful message instead of "Operation WaitForResult timed out" when the WebSocketSession.Connect method fails ([3e8712f](https://github.com/Elympics/Unity-SDK/commit/3e8712f5bb5908806f8382a98856f574c0e17c7f))


### Features

* add operation name and ID to error messages of timed out WebSocket opeartions ([9bcb53d](https://github.com/Elympics/Unity-SDK/commit/9bcb53dfcc17048d3fd851d30fae3fdc6543d518))



## [0.21.0](https://github.com/Elympics/Unity-SDK/compare/v0.20.0...v0.21.0) (2025-10-08)


### Features

* add verified nicknames ([d78ebe2](https://github.com/Elympics/Unity-SDK/commit/d78ebe203c9337f7b7ce030c1c0deac58e063266))
* mark LeaderboardClient as obsolete because it was replaced by PlayPadCommunicator.LeaderboardCommunicator some time ago ([8d4f793](https://github.com/Elympics/Unity-SDK/commit/8d4f7934669cc72dafb64b3ffc12836e70996fca))
* return BuildReport from building server ([dd35ee8](https://github.com/Elympics/Unity-SDK/commit/dd35ee83b7f962d156d92ed16319b232f22d9f4b))



## [0.20.0](https://github.com/Elympics/Unity-SDK/compare/v0.19.0...v0.20.0) (2025-09-25)


### Bug Fixes 🪲

* add a delay to createOffer to wait for ICE to gather candidates ([eb6b948](https://github.com/Elympics/Unity-SDK/commit/eb6b948251c65b6ff5a5953bf92be8665390766e))
* improve WebRTC initial connectivity ([4f1b4fe](https://github.com/Elympics/Unity-SDK/commit/4f1b4feffc881f6f20937e6f99ca4d60d57d6bdd))
* make MessagePack resolver used consistent between AOT and Editor, introduce a custom formatter for missing types ([b921617](https://github.com/Elympics/Unity-SDK/commit/b9216175837f85205675715f9d7164780ceb8754))
* missing MessagePack AOT ([aab3aa8](https://github.com/Elympics/Unity-SDK/commit/aab3aa802e5413c8e0b3c865f59a82f4778644a3))
* optimize replay data size ([55bd5ce](https://github.com/Elympics/Unity-SDK/commit/55bd5ced9706002cfdf9a15f064dc3a9b92e62e4))
* send candidates alongside offer ([ca5d450](https://github.com/Elympics/Unity-SDK/commit/ca5d450ab7e73f01c20898fa76491edc2dbf6fe5))


### Features

* add additional BuildOptions for server build ([e8d0177](https://github.com/Elympics/Unity-SDK/commit/e8d0177a8d266baad594ce6bfe801ea771ddbdf6))
* add custom player data that each player can set for themselves in a room (see UserInfo.CustomPlayerData and IRoom.UpdateCustomPlayerData) ([112233b](https://github.com/Elympics/Unity-SDK/commit/112233b69f2c1b67f2daa6107298d3b4e856292a))
* add XML comments to events and methods of IRoomsManager ([d74c77e](https://github.com/Elympics/Unity-SDK/commit/d74c77e725f71dabf51678572de1c2bb3e1da370))
* allow configuring custom prize distribution in rolling tournaments ([bdfffb9](https://github.com/Elympics/Unity-SDK/commit/bdfffb930e9164cb938584fa4bb6c411826277f2))



## [0.19.0](https://github.com/Elympics/Unity-SDK/compare/v0.18.0...v0.19.0) (2025-08-21)


### Bug Fixes 🪲

* Don't skip snapshot metadata when comparing two snapshots ([cbc28af](https://github.com/Elympics/Unity-SDK/commit/cbc28afc8c8e5958654f6456583684569de5d1af))


### Features

* add method to update rolls tournament ([ea0e82a](https://github.com/Elympics/Unity-SDK/commit/ea0e82ae1087343eee1e1848768b3fffcc5e7c9c))
* improve tooltips in game config ([a939534](https://github.com/Elympics/Unity-SDK/commit/a939534eecd82b3991c253bd0f0c9303f1741d24))
* log a warning when a tick from a received snapshot is not present in the prediction buffer ([94ae62d](https://github.com/Elympics/Unity-SDK/commit/94ae62d123fc33882a57577a3a87e1b640acbe6b))
* refactor roombetslim ([9fda550](https://github.com/Elympics/Unity-SDK/commit/9fda550d9e68c3fb99e3fa96f61aa6f0a3a0fff4))
* rollingBet in BetDetails ([7f8cfc7](https://github.com/Elympics/Unity-SDK/commit/7f8cfc7715816268d8fec91f910b25c539f2a1fc))



## [0.18.0](https://github.com/Elympics/Unity-SDK/compare/v0.17.0...v0.18.0) (2025-07-15)


### Bug Fixes 🪲

* increase connection timeouts ([f2382af](https://github.com/Elympics/Unity-SDK/commit/f2382af3bc68dc36934acdd1c1468028bc28b3c7))
* re-design ElympicsGameConfig inspector to prevent exceptions ([6cabd4f](https://github.com/Elympics/Unity-SDK/commit/6cabd4f16bcf71f32a17303c79a2ba2ec06d209e))

### Features

* BREAKING CHANGE: mark `ElympicsPlayPad.ExternalCommunicators.VirtualDeposit.IExternalBlockChainCurrencyCommunicator.EnsureVirtualDeposit` as obsolete and make PlayPad SDK handle ensuring deposit internally
* BREAKING CHANGE: rename `tournamentDetails` parameter in `Elympics.IRoomsManager.CreateAndJoinRoom` method to `competitivenessConfig`
* add automatic signing of proof of entry when player is joining a rolling tournament on EVM
* allow setting active tournament using its ID by calling `ElympicsPlayPad.ExternalCommunicators.Tournament.IExternalTournamentCommunicator.SetActiveTournament`
* expose number of new rolling tournament settlements with `ElympicsPlayPad.ExternalCommunicators.Tournament.IExternalTournamentCommunicator.GetTournamentSettlementStatus`



## [0.17.0](https://github.com/Elympics/Unity-SDK/compare/v0.16.1...v0.17.0) (2025-07-03)


### Bug Fixes 🪲

* prize amount format in rolling tournaments fee request ([850aa10](https://github.com/Elympics/Unity-SDK/commit/850aa104920dd274b586f031a913cc6494541c50))


### Features

* BREAKING CHANGE: rename `WeiConverter` to `Elympics.Util.RawCoinConverter` and update its members to use term "raw" instead of "Wei"



## [0.16.1](https://github.com/Elympics/Unity-SDK/compare/v0.16.0...v0.16.1) (2025-07-02)


### Bug Fixes

* Correctly reset room joining state (to "not joined") in case of errors ([f57f290](https://github.com/Elympics/Unity-SDK/commit/f57f290b08c04a470acb8b74369e055d69a7ea5d))


### Features

* Add `JoinCodeMismatch` to recognized lobby error kinds ([a69100c](https://github.com/Elympics/Unity-SDK/commit/a69100c3f37b61ab119af9d388f3f6cd38f2e226))



## [0.16.0](https://github.com/Elympics/Unity-SDK/compare/v0.15.4...v0.16.0) (2025-06-27)


### Bug Fixes 🪲

* matchmaking cancellation and reconnections ([597dd91](https://github.com/Elympics/Unity-SDK/commit/597dd911fc3d3e8106b1b89a9149f5ccbffd5f69))
* room tracking, joining and leaving ([88ca228](https://github.com/Elympics/Unity-SDK/commit/88ca228938728f23235649ff5fcf38e5af23d59
* deadlock when recording snapshots in editor ([86de6d8](https://github.com/Elympics/Unity-SDK/commit/86de6d83d843529c16b02e230dd688616d5568be))
* dispose of handled UnityWebRequests ([c5f5a32](https://github.com/Elympics/Unity-SDK/commit/c5f5a3231d39ae904a88458e07f3abea0bd8aceb))
* game management window initialization ([83be7c3](https://github.com/Elympics/Unity-SDK/commit/83be7c35deb7a98bcdee701739caa0ecacd1edf9))
* race condition when weaving Elympics.dll ([ac44bfc](https://github.com/Elympics/Unity-SDK/commit/ac44bfc59f0646cca0a2cb3579230a715cfa14bd))
* rooms classes allow only one room to be active at once ([5dc1c6a](https://github.com/Elympics/Unity-SDK/commit/5dc1c6a655bb796821597936ae54172de7e11994))
* remove unnecessary dependencies ([3c0a03a](https://github.com/Elympics/Unity-SDK/commit/3c0a03a3d89ab01134e906b2753fcc6c74c801c2))


### Features

* add ElympicsRandom ([497c97f](https://github.com/Elympics/Unity-SDK/commit/497c97f05bb4f249a038457726b0b8cc09e69bb6))
* add elympicsuser info ([58df1f9](https://github.com/Elympics/Unity-SDK/commit/58df1f90acf4374173d6afd3cd14a6f9b9e29efa))
* add IInitializable.InitializedByServer ([4a622ee](https://github.com/Elympics/Unity-SDK/commit/4a622ee2dc0e49a4bb90da3ec646e21c0a19eea1))
* add latestRoomUpdate date in setready ([19fa6ec](https://github.com/Elympics/Unity-SDK/commit/19fa6ecbb459a1e46e7f4b3ad26842f99b57b823))
* add RoomsManager RoomBetAmountChanged public event ([d630158](https://github.com/Elympics/Unity-SDK/commit/d6301589291dde84a426f54d3c49894d3f91bb1d))
* Add RoomsManager.IsTrackingAvailableRooms property for checking if the room list updates are received ([ae8b08d](https://github.com/Elympics/Unity-SDK/commit/ae8b08d0bbdfa4dbb2e70b21408689d579bffc8d))
* add singleplayer mode ([880a615](https://github.com/Elympics/Unity-SDK/commit/880a6158aa05f6b6080718c3f89f10aa564fd93b))
* add snapshot replay ([a8f7937](https://github.com/Elympics/Unity-SDK/commit/a8f7937312c460a9e7555154e39d26a13da58c0c))
* reafctor of roomBet ([34e2557](https://github.com/Elympics/Unity-SDK/commit/34e255717dd8bd1c5c8121e0e1bc94ecc1ac3ad8))
* send full snapshots to new clients ([e12ebad](https://github.com/Elympics/Unity-SDK/commit/e12ebad9387759c0a61241269382a47314f0427f))
* send last snapshot and RPCs after EndGame ([947d0a3](https://github.com/Elympics/Unity-SDK/commit/947d0a3de459c4a4d5e4ca23a7e7501301352d30))



## [0.15.4](https://github.com/Elympics/Unity-SDK/compare/v0.15.3...v0.15.4) (2025-04-09)


### Bug Fixes 🪲

* add a newline before the context is appended to log messages ([66259f6](https://github.com/Elympics/Unity-SDK/commit/66259f6496b659a45aa72a2a8700b5e1f795ad60))
* fix log timestamp being frozen ([d34bb52](https://github.com/Elympics/Unity-SDK/commit/d34bb5296f26ed729e367c06bb12b8d7b22e35f7))
* fix queue name being put in "Region name" field in logs ([e94619d](https://github.com/Elympics/Unity-SDK/commit/e94619dbeca838fdb6b324d37c8645b58d0e2d3f))


### Features

* add support for user avatars ([6ed20e7](https://github.com/Elympics/Unity-SDK/commit/6ed20e761cb05d7c56107d484d814daf35cf8daf))



## [0.15.3](https://github.com/Elympics/Unity-SDK/compare/v0.15.2...v0.15.3) (2025-02-26)


### Bug Fixes 🪲

* add invariantCulture option for log times ([d0e3660](https://github.com/Elympics/Unity-SDK/commit/d0e36600e942df21a9d2231d2cfe891f526a9bdc))
* fix internal guid creation ([358ced5](https://github.com/Elympics/Unity-SDK/commit/358ced5cbe8906594a55fbe72fe486c08e58ce61))
* fix physics when prediction is off ([af0a045](https://github.com/Elympics/Unity-SDK/commit/af0a0451122bc04abf77f6e64f230bc2b3e0b15f))
* improve exception handling when starting a quick match ([4c79591](https://github.com/Elympics/Unity-SDK/commit/4c795916d3447df9326f85e70e0ce36f08b24df6))
* make internal logger thread safe ([9b407b4](https://github.com/Elympics/Unity-SDK/commit/9b407b4b37845e87decf6b6933d81a3cb240e8f4))
* improve control over default server handler ([957367e](https://github.com/Elympics/Unity-SDK/commit/957367e2028bcaa6f4357446b0c4d5486723066e))
* refactor internal contextBuilder method names ([ff8f03a](https://github.com/Elympics/Unity-SDK/commit/ff8f03ac4e4c7625044370ef10a6c81153ade73e))



## [0.15.2](https://github.com/Elympics/Unity-SDK/compare/v0.15.1...v0.15.2) (2025-02-13)


### Bug Fixes 🪲

* call IClientHandlerGuid.OnConnectingFailed ([c58f0a0](https://github.com/Elympics/Unity-SDK/commit/c58f0a0041b145377f43dbc8169d66423f10264c))
* call IClientHandlerGuid.OnDisconnectedByServer handler ([a3a9f6a](https://github.com/Elympics/Unity-SDK/commit/a3a9f6aa654b9a55b073b03de585223e94dc369a))


### Features

* add retry option for webrtc connection ([cab8807](https://github.com/Elympics/Unity-SDK/commit/cab8807679e8090ea67f6ccd2d6cb613f7798407))



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
