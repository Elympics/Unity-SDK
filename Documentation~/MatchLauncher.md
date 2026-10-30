# Match launcher

`ElympicsMatchLauncher` starts Elympics matches on request of a host and returns to the boot scene after
them. It exists in every process from before the first scene loads. It has no game object and needs no
scene setup. A host can be:

- **in-process C#**: a game's own menu or PlayPad, calling `ElympicsMatchLauncher.Instance`;
- **a web page** embedding a WebGL build, through `window.elympicsHost`;
- **a native app** embedding Unity as a library (iOS, Windows, Linux, macOS), through the C API in
  `Runtime/Launcher/Native/ElympicsHost.h`.

Every host gets the same commands, events and error codes.

## Lifecycle

```
Idle ──launch──▶ Loading ──joined──▶ Running ──match ended / disconnected──▶ Ended
  ▲                 │                   │                                     │
  └──── idle ◀── Unloading ◀────────────┴──── return to boot / post-match timeout
```

- A launch is accepted only in `Idle`. Otherwise it fails with `Busy`.
- A return to boot is accepted in every state, whether the match finished or is still loading or running. It tears the match
  down, loads the boot scene and completes in `Idle`.
- After the match ends, the gameplay scene stays loaded (`Ended`) until the host returns to boot or the
  post-match timeout elapses.
- `idle` is always the last event of a cycle. The soft cycle `return to boot → idle → launch` is the normal way to
  play match after match in one process.

## Launching from C#

There is one method per match mode. Each takes the game ID (an `ElympicsGameConfig` in
`ElympicsConfig > Available games`), the mode's launch data and optional `ElympicsLaunchOptions`:

```csharp
var launcher = ElympicsMatchLauncher.Instance;

var result = await launcher.LaunchOnlineAsync(gameId, new OnlineLaunchData(
    serverAddress: matchmaking.WebServerAddress,
    useTcpUdpInsteadOfWebRtc: null,              // null: ElympicsGameConfig decides
    matchId: matchmaking.MatchId,
    userSecret: matchmaking.UserSecret,
    userId: myUserId,
    matchedPlayers: matchmaking.MatchedPlayers,
    regionName: matchmaking.RegionName));

if (!result.Success)
    Debug.LogWarning(result);                    // e.g. "Launch … failed: ConnectionFailed: …"

// Later, back to the boot scene:
await launcher.ReturnToBootAsync();
```

| Method | Launch data | Notes |
|---|---|---|
| `LaunchOnlineAsync` | `OnlineLaunchData` (required) | Joins a match on an Elympics game server |
| `LaunchSinglePlayerAsync` | `SinglePlayerLaunchData` (optional) | Server simulated in process. Bots come from `TestPlayers` |
| `LaunchHalfRemoteClientAsync` | `HalfRemoteClientLaunchData` (optional) | Joins a half-remote server. WebGL players include it only with the `ELYMPICS_WEBGL_HALF_REMOTE` scripting define, otherwise `UnsupportedMode` |
| `LaunchHalfRemoteServerAsync` | `HalfRemoteServerLaunchData` (optional) | Not available on WebGL and iOS (`UnsupportedMode`) |
| `LaunchSnapshotReplayAsync` | `SnapshotReplayLaunchData` (required) | Replays a recorded match |

The task completes when the match is joined (`Success`) or the launch fails. Its `CancellationToken`
only stops waiting. It does not cancel the launch: use `ReturnToBootAsync` for that.

### Launch data

All launch data is immutable and validated at launch.

**`OnlineLaunchData`**: every field is required, except the region, the transport and the player data.

| Field | Meaning |
|---|---|
| `ServerAddress` | With WebRTC, the web signaling address (absolute, or relative to the game servers endpoint). With TCP/UDP, `ip:port` |
| `UseTcpUdpInsteadOfWebRtc` | `null` means `ElympicsGameConfig.UseWeb` decides. TCP/UDP is `UnsupportedMode` on WebGL |
| `MatchId`, `UserSecret` | From matchmaking. See [the two secrets](#userSecret) |
| `UserId`, `MatchedPlayers` | The local player must be one of the players. There must be no duplicates and no more than `MaxPlayers` players. The order defines player indices |
| `RegionName`, `GameEngineData`, `MatchmakerData` | Optional |

**`SinglePlayerLaunchData`**: every field is optional.

| Field | Default |
|---|---|
| `MatchId` | `Guid.Empty` |
| `UserId` | `Guid.Empty` |
| `QueueName` | `"Solo"` |
| `RegionName` | none |
| `GameEngineData`, `MatchmakerData` | empty |

**`HalfRemoteClientLaunchData`**: every field is optional. `null` means the application parameter,
otherwise `ElympicsGameConfig`.

| Field | Falls back to |
|---|---|
| `ServerAddress` (`ip:port`) | `ELYMPICS_HALF_REMOTE_IP` / `_PORT`, else the config IP and the port of the transport in use |
| `UseTcpUdpInsteadOfWebRtc` | `UseWeb` |
| `PlayerIndex` | `ELYMPICS_HALF_REMOTE_PLAYER_INDEX`, else the config player index |
| `RegionName`, `QueueName` | the config's test match data |
| `GameEngineData`, `MatchmakerData` of the local player | its entry in the config's test players |
| `CustomMatchmakingData` | `ELYMPICS_CUSTOM_MATCHMAKING_DATA`, else the config's test match data |

The player list itself always comes from `TestPlayers`, as the server derives the same player IDs from its own config.
The client doesn't receive the server's custom matchmaking data, so give both of them the same value.

**`HalfRemoteServerLaunchData`**: every field is optional.

| Field | Meaning | Default |
|---|---|---|
| `Protocols` | `TcpUdp`, `WebRtc` or `Both` | `Both` |
| `ServerHostname` | IP address to listen on | the config IP |
| `TcpUdpPort` | used only with `TcpUdp` (ignored with a warning otherwise) | the config TCP port |
| `SignalingPort` | used only with `WebRtc` (ignored with a warning otherwise) | the config web port |
| `MaxPlayers` | how many test players the match uses, at most `ElympicsGameConfig.MaxPlayers` | all of them |
| `CustomMatchmakingData` | string pairs of the match, e.g. `:pub:seed` for `InitialMatchPlayerDatasGuid.Seed`. An empty dictionary means none (an empty array on the wire means unset) | `ELYMPICS_CUSTOM_MATCHMAKING_DATA`, else the config's test match data |

The custom matchmaking data sources are never merged: the first one that is set is used whole.

**`SnapshotReplayLaunchData`**: `Path` is one of:

- an http(s) URL, downloaded while the launch is loading. This works on every platform.
- a local replay file, with or without its extension. On WebGL, a local path is `UnsupportedMode`.
- a directory ending with a separator, in which case its newest replay is used.

The replay must have been recorded with the game's current version.

### Launch options

| `ElympicsLaunchOptions` | Default |
|---|---|
| `RequestId` | generated. Echoed in every event of the launch |
| `LaunchTimeout` | 60 s (`ElympicsLaunchDefaults.LaunchTimeoutSeconds`). `Timeout.InfiniteTimeSpan` means never |
| `PostMatchTimeout` | 300 s. `Timeout.InfiniteTimeSpan` means wait in `Ended` forever |

## Launching from a web page

Once the build has booted, it installs `window.elympicsHost` and emits `ready`. Wait for that event before
calling anything:

```js
window.addEventListener('elympics', (e) => {
    const { type, requestId, payload } = e.detail;
    if (type === 'ready') {
        window.elympicsHost.launchSinglePlayer(payload.gameIds[0], null, { requestId: 'first' });
    }
});
```

| Method | Arguments |
|---|---|
| `launchOnline`, `launchSinglePlayer`, `launchHalfRemoteClient`, `launchHalfRemoteServer`, `launchSnapshotReplay` | `(gameId, data?, options?)` |
| `returnToBoot`, `ping` | `(requestId?)` |
| `quitApplication` | `(reason?)` |
| `onEvent` | a handler `(type, requestId, payload, payloadJson) => void`. A handler set before the build booted is kept |

- `data` and `options` are objects or JSON strings. `null` or an omitted argument means the defaults.
- The fields are the launch data's, in camelCase (e.g. `serverAddress`, `matchedPlayers`).
- JSON has no nullables, so an unset field is `""`, `[]` or `0`, and `-1` for `playerIndex`.
- The transport is `transport: "" | "webRtc" | "tcpUdp"`, and the protocols are
  `protocols: "" | "tcpUdp" | "webRtc" | "both"`.
- `gameEngineData` is base64. A `Uint8Array` is converted for you.
- `customMatchmakingData` is an array of `{ key, value }` strings, e.g. `[{ "key": ":pub:seed", "value": "42" }]`.
  A key must not be empty or repeated.
- The options are `{ requestId, launchTimeoutSeconds, postMatchTimeoutSeconds }`. For both timeouts, `0`
  means the default and `-1` means never.

Every event is delivered to `onEvent` and dispatched as an `elympics` `CustomEvent` on `window`.

## Launching from a native app

`ElympicsHost.h` declares the C API, and `ElympicsHost.cpp` is its only source.

- **iOS:** the plugin is compiled into the app (`__Internal`).
- **Desktop:** build the `ElympicsHost` shared library with `.scripts/native/ElympicsHost/CMakeLists.txt` and ship it next to the player.
- **Without the library:** the bridge stays off and logs that.

```c
static void OnEvent(const char *type, const char *requestId, const char *payloadJson) { /* copy the strings */ }

ElympicsHost_RegisterEventHandler(OnEvent);   // before the first command, to receive "ready"
ElympicsHost_LaunchSinglePlayer("a1b2c3d4-…", "", "{\"requestId\":\"first\"}");
ElympicsHost_ReturnToBoot("back");
```

- `ElympicsHost_LaunchOnline`, `_LaunchSinglePlayer`, `_LaunchHalfRemoteClient`, `_LaunchHalfRemoteServer`
  and `_LaunchSnapshotReplay` take `(gameId, dataJson, optionsJson)`, with the same JSON as the web page.
- `ElympicsHost_ReturnToBoot` and `_Ping` take a request ID. `ElympicsHost_QuitApplication`
  takes a reason.
- Commands may be called from any thread, even before Unity has started. They are queued and run on
  Unity's main thread in order. Events arrive on Unity's main thread.
- The API version is 3 (`ElympicsHost_GetApiVersion`). The SDK refuses a plugin of another version.

## Events

Every event is `(type, requestId, payloadJson)`, and `requestId` is the ID of the command that caused it.
`launchAccepted` and `returnToBootAccepted` mean the action started, never that it finished.

| Type | Payload | When |
|---|---|---|
| `ready` | `{ sdkVersion, protocolVersion, gameIds[], warnings[] }` | once, at startup. `warnings` lists configuration problems, e.g. an unusable boot scene |
| `launchAccepted` | — | the launch was validated and its scene is loading |
| `launchSucceeded` | `{ gameId, matchId }` | the match was joined |
| `launchFailed` | `{ code, message }` | the launch failed, at validation or later. Terminal for that request |
| `returnToBootAccepted` | — | teardown is starting |
| `returnToBootFailed` | `{ code, message }` | only `InternalError` |
| `matchEnded` | `{ matchId }` | the server ended the match. The state is `Ended` |
| `disconnected` | `{ matchId, byServer }` | the connection was lost mid-match. The state is `Ended` |
| `matchEndTimedOut` | `{ matchId }` | the post-match timeout elapsed, and the launcher returns to boot on its own |
| `idle` | — | back in the boot scene, and the teardown is complete |
| `quitting` | `{ reason }` | right before the process exits |
| `pong` | — | the reply to `ping` |

`protocolVersion` (`ElympicsHostProtocol.Version`, now `3.0.0`) describes the wire format of this build.
Requests carry no version, so a host that needs one checks it on `ready`.

### Error codes

`code` is the name of an `ElympicsLaunchErrorCode`:

| Code | Meaning |
|---|---|
| `InvalidPayload` | malformed JSON, a missing or unparseable field, or data the game or mode rejects (e.g. a replay of another game version) |
| `UnknownGame` | the game ID is not among the available games, or that game has no gameplay scene |
| `UnsupportedMode` | not supported on this platform (a half-remote server on WebGL or iOS, TCP/UDP or a local replay path on WebGL, a half-remote client in a WebGL player without `ELYMPICS_WEBGL_HALF_REMOTE`) |
| `SceneLoadFailed` | the gameplay scene could not be loaded (is it in Build Settings?) or initialized |
| `ConnectionFailed` | connecting to or joining the game server failed, or a replay download failed |
| `Timeout` | the match was not joined within the launch timeout |
| `Busy` | a match already exists, or the process is limited to its `ELYMPICS_LAUNCH_MODE` match |
| `Aborted` | a return to boot or quit arrived while the launch was loading |
| `InternalError` | an unexpected exception |

<a id="userSecret"></a>
### The two "secrets"

`userSecret` in `OnlineLaunchData` is a **per-match credential for the game server**, handed out by
matchmaking together with the match ID. It is unrelated to any account-level Elympics client secret or
bearer token. Those are never part of launch data, and no bearer token ever crosses the host boundary.

## The boot scene

After a match, the launcher loads `ElympicsConfig > Boot scene`. If that scene is unset, missing from Build
Settings, or is a gameplay scene, the launcher returns to a generated empty scene instead. It logs a warning
and reports it in `ready.warnings`.

Client builds fail if the boot scene is set but missing from Build Settings, or is a gameplay scene. An unset
boot scene is only a build warning: it is the supported way to boot straight into a gameplay scene.

## Starting a match without a host: `ELYMPICS_LAUNCH_MODE`

The `ELYMPICS_LAUNCH_MODE` parameter (`--elympics-launch-mode=…`, an environment variable, or the URL query
`elympicsLaunchMode`) starts a match as soon as the gameplay scene loads. Its values are `halfRemoteClient`,
`halfRemoteServer`, `singlePlayer` and `snapshotReplay`.

Its data comes from the half-remote parameters (`--half-remote-ip`, `--half-remote-port`,
`--half-remote-player-index`), from `ELYMPICS_CUSTOM_MATCHMAKING_DATA` (see below) and from `ELYMPICS_REPLAY_PATH`
(`--elympics-replay-path`, a path or URL). Everything else comes from `ElympicsGameConfig`.

`ELYMPICS_CUSTOM_MATCHMAKING_DATA` (`--elympics-custom-matchmaking-data=…`, or the URL query `elympicsCustomMatchmakingData`)
sets the custom matchmaking data of a half-remote server or client, e.g. `--elympics-custom-matchmaking-data=:pub:seed=42;mode=fast`.
Pairs are separated by `;`, and each key is separated from its value by the first `=`. Percent-encode `;` (`%3B`) in keys and values,
`=` (`%3D`) in keys and `%` (`%25`); every key and value is percent-decoded. An entry without `=`, an empty key or a repeated key is an error.
In a URL query, the value is URL-decoded first, so encode it once more: `%253B`, `%253D`, `%2525`, `%252C` for a `,`
(a raw one cuts the value), and `%2B` for a `+`. For example, `elympicsCustomMatchmakingData=%3Apub%3Aseed%3D42%3Bmode%3Dfast`.

It limits the process to that one match:

- The launcher is passive: no bridge and no `ready`.
- Every launch fails with `Busy`.
- The process quits when the match ends or fails to start. In the Editor, play mode exits.

If the process starts in another scene (for example a boot or lobby scene), Elympics loads the gameplay scene of
the active game first. In a build, that scene must be in the build's scene list; otherwise an error is logged and
the player exits with code 1. The Editor loads it even when it's missing from Build Settings.

Typical uses:

- **Server builds:** a `UNITY_SERVER` build becomes a half-remote server only with
  `--elympics-launch-mode=halfRemoteServer`. Otherwise it waits for a host, like any build.
- **Play in the Editor.**
- **Client builds.**

Pressing Play in a gameplay scene without the parameter starts nothing. It logs that the scene awaits a launch call.

### Multiplayer Play Mode virtual players

A Multiplayer Play Mode virtual player (an additional Editor instance, Window > Multiplayer > Multiplayer Play Mode)
gets launch arguments from Elympics. They beat environment variables and lose to the real command line. By default it gets
`--elympics-launch-mode=halfRemoteClient --half-remote-player-index=<N - 2>`, where N comes from the `-name PlayerN`
argument that Multiplayer Play Mode gives it, so Player2 is player 0, Player3 is player 1 and so on.

So a virtual player joins the half-remote server as that player when play mode starts, and exits play mode when the
match ends. It opens the gameplay scene itself, because Multiplayer Play Mode gives it the scenes open in the main Editor. Player tags (Project Settings > Multiplayer > Play Mode > Player Tags) with the `Elympics:`
prefix change that:

| Tag | Effect |
|---|---|
| `Elympics:manual` | No launch arguments; launch from the Match Launcher as in the main Editor. |
| `Elympics:halfRemoteServer` (or another launch mode) | Sets the launch mode. |
| `Elympics:PlayerIndex=2` | Sets the half-remote player index. |
| `Elympics:<long-argument>=<value>` | Sets any other parameter by its long argument name, e.g. `Elympics:elympics-custom-matchmaking-data=:pub:seed=42`. |

Multiplayer Play Mode supports up to three virtual players, so the main Editor plus three virtual players can run a
half-remote server and three clients.

## In the Editor: Tools > Elympics > Match Launcher

- **Launch data:** one tab per mode, each with its own data JSON in the same wire form as the web page. A
  template sets every field as unset, so the game config fills it; the replay template is the config's snapshot
  file path. The options JSON and the game ID are shared by all modes.
- **Launch button:** named after the open tab (`▶ Online`, `▶ Single player`, `▶ HR server`, `▶ HR client`,
  `▶ Replay`). It sends that tab's data, the options and the game ID exactly as written in the window. Outside
  play mode it enters play mode first, from the boot scene or from an empty scene.
- **Controls:** return to boot, ping and quit. The window logs every command and event as one selectable text; `Copy all`
  copies the whole log.
- **With the launch mode parameter set** (e.g. in a virtual player): the launch controls are disabled, with a note
  naming the parameter's source.
- **The gameplay scene must be enabled in Build Settings**, in the Editor too. The launcher loads it by path,
  so a launch fails with `SceneLoadFailed` otherwise. The window says so when the selected game's scene is missing.

The samples launch the same way. Open a sample's boot or gameplay scene and use the window.

## Quitting, per platform

`quitApplication` is a backup escape hatch. The normal reset is `returnToBoot` and waiting for `idle`. Quit
means something different on every platform:

| Platform | What quitting does | What the host must do |
|---|---|---|
| **WebGL** | Stops the player loop. **The page, the canvas and the wasm heap survive** | On `quitting`, call `unityInstance.Quit()` and drop the reference. Create a new instance to start again |
| **iOS** | Exits the process. Apple treats a self-terminating app as a crash, and the user sees the app vanish | Prefer `returnToBoot`. Quit only when the host itself is tearing down the Unity view |
| **Desktop** | Exits the process | Relaunch the executable |
| **Editor** | Leaves play mode | — |

Teardown before a quit is best effort and lasts at most `ElympicsLaunchDefaults.QuitGraceSeconds`.

## WebGL memory

- **The heap never shrinks.** A page session's memory floor is set by the heaviest single match it has run,
  not by the average. This is not a leak: one heavy match followed by many light ones keeps the heavy
  footprint for the whole session.
- **iOS Safari budget:** stay under ~350 MB. Safari kills the tab under memory pressure from ~500 MB.
  The ~1.4 GB figure that circulates is an allocation ceiling, not a budget.
- **Mixed games on iOS:** if a host mixes memory-heavy and light games in one page session, consider
  `unityInstance.Quit()` and a new instance when switching between them, not only for recovery. On desktop,
  the soft `returnToBoot → idle → launch` cycle is correct and quitting is unnecessary.
- **Unity 6 memory settings:** the heap can grow up to 16 GB. Use Memory Growth Mode and Initial / Maximum Memory Size
  in Player Settings, and tune Initial Memory Size to the steady-state peak. `unityInstance.Quit()` reclaims the heap in
  release builds; a development-build regression prevents a full release.
