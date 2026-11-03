# Game start

The server ticks from its initialization, but the game starts only when every human player has confirmed that
the game launched for them. Until then, no inputs are applied and no score is sent.

## In a game

1. Call `IElympics.MarkReady()` on every player client once the game is playable, for example in
   `IInitializable.Initialize`:

   ```csharp
   public void Initialize()
   {
       if (Elympics.IsClient)
           Elympics.MarkReady();
   }
   ```

   Bots are ready automatically. On a server, `MarkReady()` has an effect only in local modes, where it marks all
   players ready.
2. Start gameplay in `IGameStartHandler.OnGameStarted(long startTick)`, or check `IElympics.IsGameStarted` in
   `ElympicsUpdate`. `OnGameStarted` runs once, on the server and on player clients, before `ElympicsUpdate`
   of the start tick. Inputs, `ElympicsInstantiate` and `ElympicsDestroy` aren't available there.
   - It isn't called for objects spawned after the start.
   - Like `OnServerInit` and other one-time calls, it isn't called again when a client resimulates across the start,
     so predicted state set there is wrong until
     a server snapshot from the start on arrives. For predicted gameplay, check `IsGameStarted` in
     `ElympicsUpdate` instead.

Before the start:

- Inputs for ticks before `IElympics.GameStartTick` are ignored.
- Player-to-server RPCs sent before the start are ignored by the server.
- `ElympicsScore` doesn't send anything. At the start, it sends the scores of all players, and gameplay time
  counts from there.

## Ending a match

| Situation | Result sent |
| --- | --- |
| Not all players ready within `readyTimeoutSeconds` (game config, at least 180 s) | none |
| `EndGame(...)` before the start | none (the result is ignored) |
| `EndGame()` without a result after the start | the latest `ElympicsScore` |
| Players leave after the start (`terminateWhenPlayersLeave`: `Any`, `All` or `None`) | as `EndGame()` |

`gameStartDelaySeconds` sets how far ahead the start is scheduled once everyone is ready. It's at least the total
prediction limit plus 2 ticks. `ElympicsGameConfig.GameStartDelaySeconds` returns the effective value.

## Migrating from `DefaultServerHandler`

`DefaultServerHandler` and its object in the `Elympics` prefab were removed.

- Its start timeout is replaced by `readyTimeoutSeconds`.
- Its `autoTerminationOnLeft` is replaced by `terminateWhenPlayersLeave` in the game config. **The default is
  now `All`**, where the handler used `Any`. Set it again in every game config. A copied handler (such as an
  imported copy of the PlayPad sample's `GenericServerHandler`) that ends the match itself conflicts with it: remove that
  logic and report points through `ElympicsScore`, which the SDK sends when players leave.
- A class that derived from it must implement `IServerHandlerGuid` (and `IGameStartHandler` for the start)
  itself, on its own `ElympicsBehaviour`.
- Without `MarkReady()` calls, every match times out with no result.
