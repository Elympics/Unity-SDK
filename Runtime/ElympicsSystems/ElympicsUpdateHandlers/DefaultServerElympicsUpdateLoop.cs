using System.Collections.Generic;
using UnityEngine;

namespace Elympics.ElympicsSystems
{
    internal class DefaultServerElympicsUpdateLoop : IServerElympicsUpdateLoop
    {
        private readonly ElympicsBehavioursManager _behavioursManager;
        private readonly GameEngineAdapter _gameEngineAdapter;
        private readonly List<ElympicsInput> _inputList;
        private readonly ElympicsBase _elympicsBase;
        private readonly ElympicsGameConfig _gameConfig;

        public long Tick { get; private set; }

        public DefaultServerElympicsUpdateLoop(ElympicsBehavioursManager behavioursManager, GameEngineAdapter gameEngineAdapter, ElympicsBase elympicsBase, ElympicsGameConfig gameConfig)
        {
            _behavioursManager = behavioursManager;
            _gameEngineAdapter = gameEngineAdapter;
            _elympicsBase = elympicsBase;
            _gameConfig = gameConfig;
            _inputList = new List<ElympicsInput>();
        }

        private void SetBoundaryConditions()
        {
            _inputList.Clear();
            foreach (var (elympicPlayer, elympicDataWithTickBuffer) in _gameEngineAdapter.PlayerInputBuffers)
            {
                var currentTick = Tick;
                if (elympicDataWithTickBuffer.TryGetDataForTick(currentTick, out var input) || _gameEngineAdapter.LatestSimulatedTickInput.TryGetValue(elympicPlayer, out input))
                {
                    _inputList.Add(input);
                    _gameEngineAdapter.SetLatestSimulatedInputTick(input.Player, input);
                }
            }

            using (ElympicsMarkers.Elympics_ApplyingInputMarker.Auto())
                _behavioursManager.SetCurrentInputs(_inputList);

            _elympicsBase.InvokeQueuedRpcMessages();
            _behavioursManager.CommitVars();
        }
        public ElympicsSnapshot GenerateSnapshot()
        {
            SetBoundaryConditions();

            using (ElympicsMarkers.Elympics_ElympicsUpdateMarker.Auto())
                _behavioursManager.ElympicsUpdate();

            var snapshot = _behavioursManager.GetLocalSnapshot();
            snapshot.Tick = Tick;
            snapshot.TickToPlayersInputData = GetTicksToPlayerInputs();
            return snapshot;
        }

        public void FinalizeTick(ElympicsSnapshot fullSnapshot)
        {
            Tick++;

            using (ElympicsMarkers.Elympics_ProcessSnapshotMarker.Auto())
                if (ShouldSendSnapshot(fullSnapshot.Tick))
                {
                    // gather state info from scene and send to clients
                    var sanitizedSnapshotsPerPlayer = _behavioursManager.GetSnapshotsToSend(fullSnapshot, _gameEngineAdapter.Players);

                    _gameEngineAdapter.SendSnapshotsToPlayers(sanitizedSnapshotsPerPlayer);
                }
        }
        public void HandleRenderFrame(in RenderData renderData)
        { }

        private Dictionary<int, TickToPlayerInput> GetTicksToPlayerInputs()
        {
            Dictionary<int, TickToPlayerInput> ticksToPlayerInputs = new(_gameEngineAdapter.PlayerInputBuffers.Count);
            foreach (var (player, inputBufferWithTick) in _gameEngineAdapter.PlayerInputBuffers)
            {
                var tickToPlayerInput = new TickToPlayerInput
                {
                    Data = new Dictionary<long, ElympicsSnapshotPlayerInput>(Mathf.Max(0, inputBufferWithTick.Count)),
                };
                for (var i = inputBufferWithTick.MinTick; i <= inputBufferWithTick.MaxTick; i++)
                    if (inputBufferWithTick.TryGetDataForTick(i, out var elympicsInput))
                    {
                        var snapshotInputData = new ElympicsSnapshotPlayerInput
                        {
                            Data = new List<KeyValuePair<int, byte[]>>(elympicsInput.Data)
                        };
                        tickToPlayerInput.Data.Add(i, snapshotInputData);
                    }
                ticksToPlayerInputs[(int)player] = tickToPlayerInput;
            }

            return ticksToPlayerInputs;
        }

        private bool ShouldSendSnapshot(long tick) => tick % _gameConfig.SnapshotSendingPeriodInTicks == 0;
    }
}
