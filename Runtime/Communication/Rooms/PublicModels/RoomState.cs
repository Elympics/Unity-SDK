using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Rooms.Models;

#nullable enable

namespace Elympics
{
    public class RoomState
    {
        public string RoomName { get; private set; }
        public string? JoinCode { get; private set; }
        public bool PrivilegedHost { get; private set; }
        public bool IsPrivate { get; private set; }
        public IReadOnlyList<UserInfo> Users => _users;
        public UserInfo Host => _users[0];

        public RoomMatchmakingData? MatchmakingData { get; private set; }

        private readonly List<UserInfo> _users = new();

        private readonly Dictionary<string, string> _customData = new();
        public IReadOnlyDictionary<string, string> CustomData => _customData;

        private DateTime _lastUpdate = DateTime.MinValue;

        internal RoomState(RoomStateChanged state) => Update(state);
        internal RoomState(PublicRoomState state) => Update(state);

        internal void Update(RoomStateChanged stateUpdate, in RoomStateDiff stateDiff)
        {
            stateDiff.Reset();
            if (stateUpdate.LastUpdate <= _lastUpdate)
            {
                ElympicsLogger.Log($"[{nameof(RoomState)}]New Room Update is outdated.{Environment.NewLine}" + $"Local Last Update {_lastUpdate:HH:mm:ss.ffff}" + $"RoomStateUpdate Last Update {stateUpdate.LastUpdate:HH:mm:ss.ffff}");
                return;
            }

            stateDiff.UpdatedState = true;
            var oldUsers = _users;
            var newUsers = stateUpdate.Users;

            if (oldUsers.Count != newUsers.Count)
                stateDiff.NewUserCount = (uint)newUsers.Count;

            if (oldUsers[0].UserId != newUsers[0].UserId)
                stateDiff.NewHost = newUsers[0].UserId;

            var isRoomCustomDataChanged = !_customData.IsTheSame(stateUpdate.CustomData);
            if (isRoomCustomDataChanged)
                CaptureDifferencesBetween(stateDiff.NewCustomRoomData, _customData, stateUpdate.CustomData);

            stateDiff.NewRoomName = !RoomName.Equals(stateUpdate.RoomName) ? stateUpdate.RoomName : null;
            stateDiff.NewIsPrivate = IsPrivate != stateUpdate.IsPrivate ? stateUpdate.IsPrivate : null;

            foreach (var oldUser in oldUsers)
            {
                var newUser = newUsers.FirstOrDefault(x => x.UserId == oldUser.UserId);
                if (newUser == null)
                {
                    stateDiff.UsersThatLeft.Add(oldUser);
                    continue;
                }

                if (newUser.IsReady != oldUser.IsReady)
                    stateDiff.UsersThatChangedReadiness.Add((newUser.UserId, newUser.IsReady));
                if (newUser.TeamIndex != oldUser.TeamIndex)
                    stateDiff.UsersThatChangedTeams.Add((newUser.UserId, newUser.TeamIndex));
            }

            foreach (var newUser in newUsers)
            {
                var oldUser = oldUsers.FirstOrDefault(x => x.UserId == newUser.UserId);
                if (oldUser != null)
                    continue;
                stateDiff.UsersThatJoined.Add(newUser);
            }

            stateDiff.UpdatedMatchmakingData = (MatchmakingData == null && stateUpdate.MatchmakingData != null) || (MatchmakingData != null && !MatchmakingData.Equals(stateUpdate.MatchmakingData));

            var cachedCustomMatchmakingData = MatchmakingData?.CustomData;
            var isCustomMatchmakingDataChanged = !cachedCustomMatchmakingData.IsTheSame(stateUpdate.MatchmakingData?.CustomData);
            if (isCustomMatchmakingDataChanged)
                CaptureDifferencesBetween(stateDiff.NewCustomMatchmakingData, cachedCustomMatchmakingData, stateUpdate.MatchmakingData?.CustomData);

            if (stateDiff.UpdatedMatchmakingData)
            {
                if ((IsMatchAvailable(stateUpdate.MatchmakingData?.MatchData) && !IsMatchAvailable(MatchmakingData?.MatchData))
                    || (IsMatchmakingFailed(stateUpdate.MatchmakingData?.MatchData) && !IsMatchmakingFailed(MatchmakingData?.MatchData)))
                    stateDiff.MatchDataArgs = new MatchDataReceivedArgs(stateUpdate.RoomId, stateUpdate.MatchmakingData!.MatchData!.MatchId, stateUpdate.MatchmakingData.QueueName, stateUpdate.MatchmakingData.MatchData);

                var oldMmState = MatchmakingData?.MatchmakingState;
                var newMmState = stateUpdate.MatchmakingData?.State;

                stateDiff.MatchmakingStarted = !oldMmState.IsInsideMatchmaking() && newMmState.IsInsideMatchmaking();
                stateDiff.MatchmakingEnded = oldMmState.IsInsideMatchmaking() && !newMmState.IsInsideMatchmaking();
            }

            Update(stateUpdate);
            return;

            static bool IsMatchAvailable(MatchData? matchData) =>
                matchData is { State: MatchState.Running, MatchDetails: not null };

            static bool IsMatchmakingFailed(MatchData? matchData) =>
                matchData is { State: MatchState.InitializingFailed };
        }


        private static void CaptureDifferencesBetween(Dictionary<string, string?> diffCapture, IReadOnlyDictionary<string, string>? oldVersion, IReadOnlyDictionary<string, string>? newVersion)
        {
            switch (oldVersion, newVersion)
            {
                case (null, null):
                    return;
                case (null, not null):
                    foreach (var (newKey, newValue) in newVersion)
                        diffCapture.Add(newKey, newValue);
                    return;
                case (not null, null):
                    foreach (var (oldKey, _) in oldVersion)
                        diffCapture.Add(oldKey, null);
                    return;
                default:
                    foreach (var (oldKey, oldValue) in oldVersion)
                        if (!newVersion.TryGetValue(oldKey, out var newValue))
                            diffCapture.Add(oldKey, null);
                        else if (!Equals(oldValue, newValue))
                            diffCapture.Add(oldKey, newValue);

                    foreach (var (newKey, newValue) in newVersion)
                        if (!oldVersion.ContainsKey(newKey))
                            diffCapture.Add(newKey, newValue);
                    return;
            }
        }

        private void Update(RoomStateChanged stateUpdate)
        {
            RoomName = stateUpdate.RoomName;
            PrivilegedHost = stateUpdate.HasPrivilegedHost;
            IsPrivate = stateUpdate.IsPrivate;
            JoinCode = stateUpdate.JoinCode;
            MatchmakingData = stateUpdate.MatchmakingData != null ? new RoomMatchmakingData(stateUpdate.MatchmakingData) : null;
            _customData.Clear();
            _customData.AddRange(stateUpdate.CustomData);
            _users.Clear();
            _users.AddRange(stateUpdate.Users);
            _lastUpdate = stateUpdate.LastUpdate;
        }

        internal void Update(PublicRoomState stateUpdate)
        {
            RoomName = stateUpdate.RoomName;
            PrivilegedHost = stateUpdate.HasPrivilegedHost;
            IsPrivate = stateUpdate.IsPrivate;
            MatchmakingData = stateUpdate.MatchmakingData != null ? new RoomMatchmakingData(stateUpdate.MatchmakingData) : null;
            _customData.Clear();
            _customData.AddRange(stateUpdate.CustomData);
            _users.Clear();
            _users.AddRange(stateUpdate.Users);
            _lastUpdate = stateUpdate.LastUpdate;
        }
    }
}
