#nullable enable

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Rooms.PublicModels;

namespace Elympics
{
    internal interface IRoomJoiner
    {
        Guid? CurrentRoomId { get; set; }

        event Action<RoomJoiningState>? JoiningStateChanged;

        UniTask<Guid> CreateAndJoinRoom(
            string roomName,
            string queueName,
            bool isSingleTeam,
            bool isPrivate,
            bool isEphemeral,
            IReadOnlyDictionary<string, string> customRoomData,
            IReadOnlyDictionary<string, string> customMatchmakingData,
            RoomBetAmount? betDetails = null,
            TournamentDetails? tournamentDetails = null);

        public UniTask<Guid> JoinRoom(Guid? roomId, string? joinCode, uint? teamIndex = null);

        void Reset();
    }
}
