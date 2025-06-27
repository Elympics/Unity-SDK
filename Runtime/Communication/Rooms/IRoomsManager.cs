using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Rooms.PublicModels;
using JetBrains.Annotations;

#nullable enable

namespace Elympics
{
    [PublicAPI]
    public interface IRoomsManager
    {
        event Action<RoomListUpdatedArgs>? RoomListUpdated;
        event Action<JoinedRoomUpdatedArgs>? JoinedRoomUpdated;

        event Action<JoinedRoomArgs>? JoinedRoom;
        event Action<LeftRoomArgs>? LeftRoom;

        event Action<UserJoinedArgs>? UserJoined;
        event Action<UserLeftArgs>? UserLeft;
        event Action<UserCountChangedArgs>? UserCountChanged;
        event Action<HostChangedArgs>? HostChanged;
        event Action<UserReadinessChangedArgs>? UserReadinessChanged;
        event Action<UserChangedTeamArgs>? UserChangedTeam;
        event Action<CustomRoomDataChangedArgs>? CustomRoomDataChanged;
        public event Action<RoomPublicnessChangedArgs>? RoomPublicnessChanged;
        public event Action<RoomNameChangedArgs>? RoomNameChanged;
        public event Action<RoomBetAmountChangedArgs>? RoomBetAmountChanged;

        event Action<MatchmakingDataChangedArgs>? MatchmakingDataChanged;
        event Action<MatchmakingStartedArgs>? MatchmakingStarted;
        event Action<MatchmakingEndedArgs>? MatchmakingEnded;
        event Action<MatchDataReceivedArgs>? MatchDataReceived;
        event Action<CustomMatchmakingDataChangedArgs>? CustomMatchmakingDataChanged;

        // TODO: getter for all rooms (available + joined)

        bool TryGetAvailableRoom(Guid roomId, out IRoom? room);
        IReadOnlyList<IRoom> ListAvailableRooms();

        [Obsolete("Only one room can be joined at once. See " + nameof(CurrentRoom))]
        bool TryGetJoinedRoom(Guid roomId, out IRoom? room);
        [Obsolete("Only one room can be joined at once. See " + nameof(CurrentRoom))]
        IReadOnlyList<IRoom> ListJoinedRooms();

        IRoom? CurrentRoom { get; }

        UniTask StartTrackingAvailableRooms();
        UniTask StopTrackingAvailableRooms();
        bool IsTrackingAvailableRooms { get; }

        UniTask<IRoom> CreateAndJoinRoom(
            string roomName,
            string queueName,
            bool isSingleTeam,
            bool isPrivate,
            IReadOnlyDictionary<string, string>? customRoomData = null,
            IReadOnlyDictionary<string, string>? customMatchmakingData = null,
            CompetitivenessConfig? tournamentDetails = null);
        UniTask<IRoom> JoinRoom(Guid? roomId, string? joinCode, uint? teamIndex = null);
        UniTask<IRoom> StartQuickMatch(
            string queueName,
            byte[]? gameEngineData = null,
            float[]? matchmakerData = null,
            Dictionary<string, string>? customRoomData = null,
            Dictionary<string, string>? customMatchmakingData = null,
            CompetitivenessConfig? tournamentDetails = null,
            CancellationToken ct = default);
        public event Func<IRoom, IRoom>? RoomSetUp;
        internal UniTask CheckJoinedRoomStatus();
        internal void Reset();
    }
}
