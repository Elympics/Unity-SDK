using System.Collections.Generic;
using UnityEngine;


namespace Elympics
{
    internal class TickEntryData
    {
        private readonly ElympicsSnapshotWithMetadata _snapshot;
        private readonly float _executionTime;
        private readonly InputInfo[] _inputInfos;


        internal long Tick => _snapshot.Tick;
        internal float ExecutionTime => _executionTime;
        internal List<ElympicsBehaviourMetadata> SynchronizedState => _snapshot.Metadata;
        internal ElympicsSnapshotWithMetadata Snapshot => _snapshot;
        internal InputInfo[] InputInfos => _inputInfos;

        internal TickEntryData(ElympicsSnapshotWithMetadata snapshot, int numberOfPlayers)
        {
            _snapshot = snapshot;
            _executionTime = (float)(snapshot.TickEndUtc - snapshot.TickStartUtc).TotalMilliseconds;

            _inputInfos = new InputInfo[numberOfPlayers];
            for (int i = 0; i < _inputInfos.Length; i++)
            {
                _inputInfos[i] = new InputInfo(snapshot, i);
            }
        }
    }

    internal struct InputInfo
    {
        private static readonly string ReceivedInputInfo = "Received ({0}B)";
        private static readonly string MissingInputInfo = "Missing";

        internal string Message { get; }
        internal Color Color { get; }

        internal InputInfo(ElympicsSnapshotWithMetadata snapshot, int i)
        {
            if (snapshot.TickToPlayersInputData.TryGetValue(i, out var tickToPlayerInput) && tickToPlayerInput.Data.TryGetValue(snapshot.Tick, out var input))
            {
                Message = string.Format(ReceivedInputInfo, input.Data.Count);
                Color = input.Data.Count > 0 ? ServerAnalyzerUtils.PositiveColor : ServerAnalyzerUtils.WarningColor;
            }
            else
            {
                Message = MissingInputInfo;
                Color = ServerAnalyzerUtils.WarningColor;
            }
        }
    }
}
