using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Rooms.PublicModels;
using Elympics.Rooms.Models;
using JetBrains.Annotations;

#nullable enable

namespace Elympics
{
    [PublicAPI]
    public interface IRoom : IDisposable
    {
        Guid RoomId { get; }
        RoomState State { get; }
        public string RoomName => State.RoomName;
        public string? JoinCode => State.JoinCode;

        bool IsDisposed { get; }
        bool IsJoined { get; internal set; }

        bool HasMatchmakingEnabled { get; }
        bool IsMatchAvailable { get; }

        UniTask UpdateRoomParams(string? roomName = null, bool? isPrivate = null, IReadOnlyDictionary<string, string>? roomCustomData = null, IReadOnlyDictionary<string, string>? customMatchmakingData = null, CompetitivenessConfig? competitivenessConfig = null);
        UniTask UpdateCustomPlayerData(Dictionary<string, string>? customPlayerData);
        UniTask ChangeTeam(uint? teamIndex);
        public UniTask BecomeSpectator() => ChangeTeam(null);

        UniTask MarkYourselfReady(byte[]? gameEngineData = null, float[]? matchmakerData = null, CancellationToken ct = default);
        UniTask MarkYourselfUnready();

        UniTask StartMatchmaking();
        UniTask CancelMatchmaking(CancellationToken ct = default);

        /// Connects to the game server if there is a match available in the room.
        /// <remarks>The method loads the gameplay scene in the non-additive mode.</remarks>
        void PlayAvailableMatch();

        UniTask Leave();

        internal void UpdateState(RoomStateChanged roomState, in RoomStateDiff stateDiff)
        { }

        internal void UpdateState(PublicRoomState roomState)
        { }

        internal bool IsQuickMatch => false;

        internal UniTask StartMatchmakingInternal() => UniTask.CompletedTask;
        internal UniTask CancelMatchmakingInternal(CancellationToken ct = default) => UniTask.CompletedTask;
    }
}
