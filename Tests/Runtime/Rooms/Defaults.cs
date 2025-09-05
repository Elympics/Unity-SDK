using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Communication.Rooms.Models;
using Elympics.Lobby;
using Elympics.Models.Authentication;
using Elympics.Rooms.Models;

#nullable enable

namespace Elympics.Tests.Rooms
{
    internal static class Defaults
    {
        public static RoomStateChanged CreateRoomState(Guid roomId, Guid hostId, MatchmakingStateDto mmState = MatchmakingStateDto.Unlocked)
        {
            var userList = new List<UserInfoDto>
            {
                new(hostId, 0, false, null, null, new Dictionary<string, string>()),
            };

            return new RoomStateChanged(roomId,
                DateTime.UtcNow,
                "Test Name",
                "test-join-code",
                true,
                CreateMatchmakingData(mmState, userList.Select(u => u.UserId).ToList()),
                userList,
                false,
                false,
                new Dictionary<string, string>());
        }

        public static MatchmakingData CreateMatchmakingData(MatchmakingStateDto state, IReadOnlyList<Guid>? matchedPlayers = null) => new(DateTime.UtcNow,
            state,
            "test-queue",
            1,
            1,
            new Dictionary<string, string>(),
            state == MatchmakingStateDto.Playing ? CreateMatchData(matchedPlayers) : null,
            null,
            null);

        public static PublicRoomState CreatePublicRoomState(Guid roomId, Guid hostId, MatchmakingStateDto mmState = MatchmakingStateDto.Unlocked) => new(roomId,
            DateTime.UtcNow,
            "Test Name",
            true,
            CreatePublicMatchmakingData(mmState),
            new List<UserInfoDto>
            {
                new(hostId, 0, false, null, null, new Dictionary<string, string>()),
            },
            false,
            new Dictionary<string, string>());

        public static PublicMatchmakingData CreatePublicMatchmakingData(MatchmakingStateDto state) => new(DateTime.UtcNow,
            state,
            "test-queue",
            1,
            1,
            new Dictionary<string, string>(),
            null);

        public static MatchDataDto CreateMatchData(IReadOnlyList<Guid>? matchedPlayers = null) => new(Guid.NewGuid(),
            MatchStateDto.Running,
            CreateMatchDetails(matchedPlayers),
            null);

        public static MatchDetailsDto CreateMatchDetails(IReadOnlyList<Guid>? matchedPlayers = null) => new(matchedPlayers ?? Array.Empty<Guid>(),
            string.Empty,
            string.Empty,
            string.Empty,
            Array.Empty<byte>(),
            Array.Empty<float>());

        public static UserInfoDto CreateUserInfo(Guid? userId = null) => new(userId ?? Guid.NewGuid(),
            0,
            false,
            string.Empty,
            null,
            null);

        public static SessionConnectionDetails CreateConnectionDetails(Guid userId, string regionName = "warsaw") => new("url",
            new AuthData(userId, string.Empty, string.Empty),
            Guid.Empty,
            string.Empty,
            regionName);

        public static RoomStateChanged WithLastUpdate(this RoomStateChanged state, DateTime lastUpdate) => state with
        {
            LastUpdate = lastUpdate,
        };

        public static RoomStateChanged WithUserTeamSwitched(this RoomStateChanged state, Guid userId, uint? teamIndex) => state with
        {
            Users = state.Users.Select(user => user.UserId != userId ? user : user with { TeamIndex = teamIndex }).ToList(),
        };

        public static RoomStateChanged WithUserAdded(this RoomStateChanged state, UserInfoDto user) => state with
        {
            Users = state.Users.Append(user).ToList(),
        };

        public static RoomStateChanged WithUserRemoved(this RoomStateChanged state, Guid userId) => state with
        {
            Users = state.Users.Where(u => u.UserId != userId).ToList(),
        };

        public static RoomStateChanged WithNameChanged(this RoomStateChanged state, string name) => state with
        {
            RoomName = name,
        };

        public static RoomStateChanged WithCustomDataAdded(this RoomStateChanged state, string key, string value) => state with
        {
            CustomData = state.CustomData
                .Where(p => p.Key != key)
                .Append(new KeyValuePair<string, string>(key, value))
                .ToDictionary(p => p.Key, p => p.Value),
        };

        public static RoomStateChanged WithCustomDataRemoved(this RoomStateChanged state, string key) => state with
        {
            CustomData = state.CustomData
                .Where(p => p.Key != key)
                .ToDictionary(p => p.Key, p => p.Value),
        };

        public static RoomStateChanged WithPublicnessChanged(this RoomStateChanged state, bool isPrivate) => state with
        {
            IsPrivate = isPrivate,
        };

        public static RoomStateChanged WithUserReadinessChanged(this RoomStateChanged state, Guid userId, bool isReady) => state with
        {
            Users = state.Users.Select(user => user.UserId != userId ? user : user with { IsReady = isReady }).ToList(),
        };

        public static RoomStateChanged WithMatchmakingData(this RoomStateChanged state, MatchmakingData mmData) => state with
        {
            MatchmakingData = mmData,
        };

        public static RoomStateChanged WithMatchmakingData(this RoomStateChanged state, Func<RoomStateChanged, MatchmakingData?> mutator) => state with
        {
            MatchmakingData = mutator(state),
        };

        public static RoomStateChanged WithNoPrivilegedHost(this RoomStateChanged state) => state with
        {
            HasPrivilegedHost = false,
        };

        public static MatchmakingData WithMatchData(this MatchmakingData mmData, MatchDataDto? matchData) => mmData with
        {
            MatchData = matchData,
        };
    }
}
