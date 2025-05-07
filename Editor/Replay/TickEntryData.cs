using System.Collections.Generic;
using UnityEngine;

namespace Elympics.Editor.Replay
{
    internal class TickEntryData
    {
        internal long Tick => Snapshot.Tick;
        internal float ExecutionTime { get; }
        internal List<ElympicsBehaviourMetadata> SynchronizedState => Snapshot.Metadata;
        internal ElympicsSnapshotWithMetadata Snapshot { get; }
        internal InputInfo[] InputInfos { get; }

        internal TickEntryData(ElympicsSnapshotWithMetadata snapshot, int numberOfPlayers)
        {
            Snapshot = snapshot;
            ExecutionTime = (float)(snapshot.TickEndUtc - snapshot.TickStartUtc).TotalMilliseconds;

            InputInfos = new InputInfo[numberOfPlayers];
            for (var i = 0; i < InputInfos.Length; i++)
            {
                InputInfos[i] = new InputInfo(snapshot, i);
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
            if (snapshot.TickToPlayersInputData != null && snapshot.TickToPlayersInputData.TryGetValue(i, out var tickToPlayerInput) && tickToPlayerInput.Data.TryGetValue(snapshot.Tick, out var input))
            {
                Message = string.Format(ReceivedInputInfo, input.Data.Count);
                Color = input.Data.Count > 0 ? EditorReplayUtils.PositiveColor : EditorReplayUtils.WarningColor;
            }
            else
            {
                Message = MissingInputInfo;
                Color = EditorReplayUtils.WarningColor;
            }
        }
    }
}
