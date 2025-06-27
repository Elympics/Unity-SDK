using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Elympics.Rooms.Models
{
    internal class RoomStateDiff
    {
        public bool UpdatedState;
        public bool InitializedState;

        public uint? NewUserCount;
        public Guid? NewHost;

        public readonly List<UserInfo> UsersThatJoined = new();
        public readonly List<UserInfo> UsersThatLeft = new();

        public readonly List<(Guid UserId, uint? TeamIndex)> UsersThatChangedTeams = new();
        public readonly List<(Guid UserId, bool IsReady)> UsersThatChangedReadiness = new();

        public bool UpdatedMatchmakingData;
        public bool MatchmakingStarted;
        public bool MatchmakingEnded;

        public readonly Dictionary<string, string?> NewCustomRoomData = new();
        public readonly Dictionary<string, string?> NewCustomMatchmakingData = new();
        public bool? NewIsPrivate;
        public string? NewRoomName;
        public bool UpdatedBetAmount;
        public (Guid CoinId, decimal BetAmount)? NewBetAmount;

        public MatchDataReceivedArgs? MatchDataArgs;

        public override string ToString() => $"{nameof(UpdatedState)}:{UpdatedState}{Environment.NewLine}"
            + $"{nameof(InitializedState)}:{InitializedState}{Environment.NewLine}"
            + $"{nameof(NewHost)}:{NewHost}{Environment.NewLine}"
            + $"{nameof(UsersThatJoined)}:{Environment.NewLine}\t{string.Join(Environment.NewLine + "\t", UsersThatJoined)}{Environment.NewLine}"
            + $"{nameof(UsersThatLeft)}:{Environment.NewLine}\t{string.Join(Environment.NewLine + "\t", UsersThatLeft)}{Environment.NewLine}"
            + $"{nameof(UsersThatChangedTeams)}:{Environment.NewLine}\t{string.Join(Environment.NewLine + "\t", UsersThatChangedTeams)}{Environment.NewLine}"
            + $"{nameof(UsersThatChangedReadiness)}:{Environment.NewLine}\t{string.Join(Environment.NewLine + "\t", UsersThatChangedReadiness)}{Environment.NewLine}"
            + $"{nameof(NewCustomRoomData)}:{Environment.NewLine}\t{string.Join(Environment.NewLine + "\t", NewCustomRoomData.Select(kv => $"Key = {kv.Value}, Value = {kv.Key}"))}{Environment.NewLine}"
            + $"{nameof(NewCustomMatchmakingData)}:{Environment.NewLine}\t{string.Join(Environment.NewLine + "\t", NewCustomMatchmakingData.Select(kv => $"Key = {kv.Value}, Value = {kv.Key}"))}{Environment.NewLine}"
            + $"{nameof(UpdatedMatchmakingData)}:{UpdatedMatchmakingData}{Environment.NewLine}"
            + $"{nameof(MatchmakingStarted)}:{MatchmakingStarted}{Environment.NewLine}"
            + $"{nameof(MatchmakingEnded)}:{MatchmakingEnded}{Environment.NewLine}"
            + $"{nameof(NewIsPrivate)}:{NewIsPrivate}{Environment.NewLine}"
            + $"{nameof(NewRoomName)}:{NewRoomName}{Environment.NewLine}"
            + $"{nameof(MatchDataArgs)}:{MatchDataArgs}{Environment.NewLine}";

        public void Reset()
        {
            UpdatedState = false;
            InitializedState = false;
            NewUserCount = null;
            NewHost = null;
            UsersThatJoined.Clear();
            UsersThatLeft.Clear();
            UsersThatChangedTeams.Clear();
            UsersThatChangedReadiness.Clear();
            NewCustomRoomData.Clear();
            NewCustomMatchmakingData.Clear();
            NewIsPrivate = null;
            NewRoomName = null;
            UpdatedBetAmount = false;
            NewBetAmount = null;

            UpdatedMatchmakingData = false;
            MatchmakingStarted = false;
            MatchmakingEnded = false;

            MatchDataArgs = null;
        }
    }
}
