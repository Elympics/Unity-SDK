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
    public interface IRoomsManager
    {
        /// <summary>Raised when the list of available rooms changes or when any of the rooms included in that list changes state.</summary>
        /// <remarks>
        /// This event can be enabled by calling <see cref="StartTrackingAvailableRooms"/> and disabled by calling <see cref="StopTrackingAvailableRooms"/>.
        /// All rooms that were created in the same region, on the same game version, are not <see cref="CurrentRoom"/>,
        /// and have <see cref="IRoom.IsQuickMatch"/> set to false are considered available.
        /// </remarks>
        /// <seealso cref="ListAvailableRooms"/>
        event Action<RoomListUpdatedArgs>? RoomListUpdated;
        /// <summary>Raised when <see cref="IRoom.State"/> of <see cref="CurrentRoom"/> changes.</summary>
        event Action<JoinedRoomUpdatedArgs>? JoinedRoomUpdated;

        /// <summary>Raised when a room is successfully joined.</summary>
        /// <seealso cref="CurrentRoom"/>
        event Action<JoinedRoomArgs>? JoinedRoom;
        /// <summary>Raised when the local player leaves a room.</summary>
        /// <seealso cref="CurrentRoom"/>
        event Action<LeftRoomArgs>? LeftRoom;

        /// <summary>Raised when another user joins <see cref="CurrentRoom"/>.</summary>
        event Action<UserJoinedArgs>? UserJoined;
        /// <summary>Raised when another user leaves <see cref="CurrentRoom"/>.</summary>
        event Action<UserLeftArgs>? UserLeft;
        /// <summary>Raised when the number of the users in <see cref="CurrentRoom"/> changes.</summary>
        event Action<UserCountChangedArgs>? UserCountChanged;
        /// <summary>Raised when the <see cref="RoomState.Host"/> of <see cref="CurrentRoom"/> changes.</summary>
        event Action<HostChangedArgs>? HostChanged;
        /// <summary>Raised when a user in th <see cref="CurrentRoom"/> changes their readiness.</summary>
        event Action<UserReadinessChangedArgs>? UserReadinessChanged;
        /// <summary>Raised when a user in th <see cref="CurrentRoom"/> changes their team.</summary>
        event Action<UserChangedTeamArgs>? UserChangedTeam;
        /// <summary>Raised when <see cref="RoomState.CustomData"/> of <see cref="CurrentRoom"/> changes.</summary>
        event Action<CustomRoomDataChangedArgs>? CustomRoomDataChanged;
        /// <summary>Raised when <see cref="UserInfo.CustomPlayerData"/> of a user in the <see cref="CurrentRoom"/> changes.</summary>
        event Action<CustomPlayerDataChangedArgs>? CustomPlayerDataChanged;
        /// <summary>Raised when <see cref="RoomState.IsPrivate"/> of <see cref="CurrentRoom"/> changes.</summary>
        public event Action<RoomPublicnessChangedArgs>? RoomPublicnessChanged;
        /// <summary>Raised when the <see cref="RoomState.RoomName"/> of <see cref="CurrentRoom"/> changes.</summary>
        public event Action<RoomNameChangedArgs>? RoomNameChanged;
        /// <summary>Raised when <see cref="RoomMatchmakingData.BetDetails"/> stored in <see cref="RoomState.MatchmakingData"/> of <see cref="CurrentRoom"/> changes.</summary>
        public event Action<RoomBetAmountChangedArgs>? RoomBetAmountChanged;
        /// <summary>Raised when <see cref="RoomState.MatchmakingData"/> of <see cref="CurrentRoom"/> changes.</summary>
        event Action<MatchmakingDataChangedArgs>? MatchmakingDataChanged;

        /// <summary>Raised when the matchmaking process starts.</summary>
        event Action<MatchmakingStartedArgs>? MatchmakingStarted;
        /// <summary>Raised when the matchmaking process ends.</summary>
        event Action<MatchmakingEndedArgs>? MatchmakingEnded;
        /// <summary>Raised when data about a match is received.</summary>
        event Action<MatchDataReceivedArgs>? MatchDataReceived;
        /// <summary>Raised when <see cref="RoomMatchmakingData.CustomData"/> stored in <see cref="RoomState.MatchmakingData"/> of <see cref="CurrentRoom"/> changes.</summary>
        event Action<CustomMatchmakingDataChangedArgs>? CustomMatchmakingDataChanged;

        // TODO: getter for all rooms (available + joined)

        /// <summary>Gets available room with ID matching <paramref name="roomId"/>.</summary>
        /// <param name="roomId"><see cref="IRoom.RoomId"/> of the available room to be found.</param>
        /// <param name="room">The available room with ID matching <paramref name="roomId"/> if found, otherwise null.</param>
        /// <returns>True if an available room with ID matching <paramref name="roomId"/> is found.</returns>
        /// <remarks>
        /// All rooms that were created in the same region, on the same game version, are not <see cref="CurrentRoom"/>,
        /// and have <see cref="IRoom.IsQuickMatch"/> set to false are considered available.
        /// </remarks>
        /// <seealso cref="ListAvailableRooms"/>
        bool TryGetAvailableRoom(Guid roomId, out IRoom? room);
        /// <summary>Returns all available rooms.</summary>
        /// <remarks>
        /// All rooms that were created in the same region, on the same game version, are not <see cref="CurrentRoom"/>,
        /// and have <see cref="IRoom.IsQuickMatch"/> set to false are considered available.
        /// </remarks>
        IReadOnlyList<IRoom> ListAvailableRooms();

        [Obsolete("Only one room can be joined at once. See " + nameof(CurrentRoom))]
        bool TryGetJoinedRoom(Guid roomId, out IRoom? room);
        [Obsolete("Only one room can be joined at once. See " + nameof(CurrentRoom))]
        IReadOnlyList<IRoom> ListJoinedRooms();

        /// <summary>The room in which the local player currently is or null if the player is not inside a room.</summary>
        /// <seealso cref="JoinRoom"/>
        /// <seealso cref="IRoom.Leave"/>
        IRoom? CurrentRoom { get; }

        /// <summary>Enables <see cref="RoomListUpdated"/>.</summary>
        UniTask StartTrackingAvailableRooms();
        /// <summary>Disables <see cref="RoomListUpdated"/>.</summary>
        UniTask StopTrackingAvailableRooms();
        bool IsTrackingAvailableRooms { get; }

        /// <summary>Creates a new room and joins it as the host.</summary>
        /// <param name="roomName">The name of the room.</param>
        /// <param name="queueName">Matchmaking queue the room should use.</param>
        /// <param name="isSingleTeam"></param>
        /// <param name="isPrivate"></param>
        /// <param name="customRoomData">Custom, game specific data about the room.</param>
        /// <param name="customMatchmakingData">Custom, game specific data passed to the matchmaking system.</param>
        /// <param name="tournamentDetails">Configuration determining competitiveness type and bet used in the room.</param>
        /// <returns>An awaitable task which returns a reference to the newly created room once it is joined by the local player.</returns>
        UniTask<IRoom> CreateAndJoinRoom(
            string roomName,
            string queueName,
            bool isSingleTeam,
            bool isPrivate,
            IReadOnlyDictionary<string, string>? customRoomData = null,
            IReadOnlyDictionary<string, string>? customMatchmakingData = null,
            CompetitivenessConfig? tournamentDetails = null);
        /// <summary>Joins the room with matching <paramref name="roomId"/> or <paramref name="joinCode"/>.</summary>
        /// <param name="roomId">ID of the room to join. When using <paramref name="joinCode"/>, this argument is optional.</param>
        /// <param name="joinCode"><see cref="IRoom.JoinCode"/>, which is necessary to join a private room.</param>
        /// <param name="teamIndex">Index of the team to join after joining the room. Only applicable to rooms with more than one team.</param>
        /// <returns>An awaitable task which returns a reference to the newly joined room once it is joined by the local player.</returns>
        UniTask<IRoom> JoinRoom(Guid? roomId, string? joinCode, uint? teamIndex = null);
        /// <summary>Create and join a new room, which will immediately start matchmaking and will be destroyed after the match ends.</summary>
        /// <param name="queueName">Name of the matchmaking queue to be used.</param>
        /// <param name="gameEngineData"></param>
        /// <param name="matchmakerData"></param>
        /// <param name="customRoomData"></param>
        /// <param name="customMatchmakingData"></param>
        /// <param name="competitivenessConfig">Configuration determining competitiveness type and bet used in the room.</param>
        /// <param name="ct"></param>
        /// <returns>An awaitable task which returns a reference to the newly created room once a match is found by the matchmaking system.</returns>
        UniTask<IRoom> StartQuickMatch(
            string queueName,
            byte[]? gameEngineData = null,
            float[]? matchmakerData = null,
            Dictionary<string, string>? customRoomData = null,
            Dictionary<string, string>? customMatchmakingData = null,
            CompetitivenessConfig? competitivenessConfig = null,
            CancellationToken ct = default);
        public event Func<IRoom, IRoom>? RoomSetUp;
        internal UniTask CheckJoinedRoomStatus(GameDataResponse gameDataResponse);
        internal void Reset();
    }
}
