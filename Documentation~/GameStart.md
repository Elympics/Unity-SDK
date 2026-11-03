# Game start

The server ticks from its initialization, but the game starts only when every human player has confirmed that
they're ready. Until then, no inputs are applied and no score is sent.

## In a game

1. Call `IElympics.MarkReady()` on every player client from a player action, such as a "Ready" or "Play" button:

   ```csharp
   public void OnReadyButtonClicked() => Elympics.MarkReady();
   ```

   Bots are ready automatically. On a server, `MarkReady()` has an effect only in local modes, where it marks all
   players ready.
2. Check `IElympics.IsGameStarted` in `ElympicsUpdate` to start gameplay. It's true from `IElympics.GameStartTick`
   on, also when a client resimulates across the start and for objects spawned after it.
3. Use `IGameStartHandler.OnGameStarted(long startTick)` for visuals only, such as hiding a "Waiting for players"
   screen. It runs once, on the server and on player clients, before `ElympicsUpdate` of the start tick, and isn't
   called again on resimulation or for objects spawned after the start. Inputs, `ElympicsInstantiate` and
   `ElympicsDestroy` aren't available there.

Before the start:

- Inputs for ticks before `IElympics.GameStartTick` are ignored.
- Player-to-server RPCs sent before the start are ignored by the server.
- `ElympicsScore` doesn't send anything. At the start, it's enabled (unless the game enabled it itself), it sends
  the scores of all players, and gameplay time counts from `IElympics.GameStartUtc`.

## Ending a match

| Situation | Result sent |
| --- | --- |
| No player connects within `noPlayerTimeoutSeconds` (game config, default 180 s) from the server init | none |
| Not all players ready within `readyTimeoutSeconds` (default 60 s) from the first player connection | none |
| `EndGame(...)` before the start | none (the result is ignored) |
| A player marks ready, then leaves before the start | the game still starts, then the leave rule below applies: with `Any` or `All`, the initial score, or the score when the rejoin timeout ends (no refund) |
| `EndGame()` without a result after the start | the latest `ElympicsScore` |
| `Any` (one) or `All` players leave after the start (`terminateWhenPlayersLeave`) and don't rejoin within `rejoinTimeoutSeconds` (default 0, ending at once) | as `EndGame()` |
| Players leave with `terminateWhenPlayersLeave` set to `None` | nothing: the game must call `EndGame()` |

`gameStartDelaySeconds` sets how far ahead the start is scheduled once everyone is ready. It's at least the total
prediction limit plus 2 ticks. `ElympicsGameConfig.GameStartDelaySeconds` returns the effective value.

## Migrating from `DefaultServerHandler`

`DefaultServerHandler` and its object in the `Elympics` prefab were removed.

- Its start timeout is replaced by `readyTimeoutSeconds` and `noPlayerTimeoutSeconds`.
- Its `autoTerminationOnLeft` is replaced by `terminateWhenPlayersLeave` in the game config. Its default is `All`,
  where the handler used `Any`, so set it in every game config. A copied handler (such as an imported copy of the
  PlayPad sample's `GenericServerHandler`) that ends the match itself conflicts with it: remove that logic, use
  `rejoinTimeoutSeconds` for a rejoin grace period, and report points through `ElympicsScore`, which the SDK sends
  when players leave.
- A class that derived from it must implement `IServerHandlerGuid` (and `IGameStartHandler` for start visuals)
  itself, on its own `ElympicsBehaviour`.
- Without `MarkReady()` calls, every match times out with no result.
