using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Util;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record MatchmakingData(
        [property: Key(0)] DateTime LastStateUpdate,
        [property: Key(1)] MatchmakingState State,
        [property: Key(2)] string QueueName,
        [property: Key(3)] uint TeamCount,
        [property: Key(4)] uint TeamSize,
        [property: Key(5)] IReadOnlyDictionary<string, string> CustomData,
        [property: Key(6)] MatchData? MatchData,
        [property: Key(7)] RoomTournamentDetails? TournamentDetails,
        [property: Key(8)] RoomBetDetails? BetDetails)
    {
        public virtual bool Equals(MatchmakingData? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return State == other.State
                && LastStateUpdate.Equals(other.LastStateUpdate)
                && QueueName == other.QueueName
                && TeamSize == other.TeamSize
                && TeamCount == other.TeamCount
                && Equals(MatchData, other.MatchData)
                && CustomData.Count == other.CustomData.Count
                && !CustomData.Except(other.CustomData).Any();
        }

        public override string ToString() => $"{nameof(LastStateUpdate)}:{LastStateUpdate::HH:mm:ss.ffff}{Environment.NewLine}"
            + $"{nameof(State)}:{State}{Environment.NewLine}"
            + $"{nameof(QueueName)}:{QueueName}{Environment.NewLine}"
            + $"{nameof(TeamCount)}:{TeamCount}{Environment.NewLine}"
            + $"{nameof(TeamSize)}:{TeamSize}{Environment.NewLine}"
            + $"{nameof(CustomData)}:{Environment.NewLine}\t{string.Join(Environment.NewLine + "\t", CustomData?.Select(kv => $"Key = {kv.Key}, Value = {kv.Value}"))}{Environment.NewLine}"
            + $"{nameof(MatchData)}:{Environment.NewLine}\t{MatchData?.ToString().Replace(Environment.NewLine, Environment.NewLine + "\t")}{Environment.NewLine}"
            + $"{nameof(BetDetails)}:{Environment.NewLine}\t{BetDetails?.ToString().Replace(Environment.NewLine, Environment.NewLine + "\t")}{Environment.NewLine}";

        public override int GetHashCode() => HashCode.Combine(State, LastStateUpdate, QueueName, TeamSize, TeamCount, MatchData, CustomData.Count);
    }

    [MessagePackObject]
    public record RoomTournamentDetails([property: Key(0)] string TournamentId, [property: Key(1)] ChainType? ChainType);

    public enum ChainType
    {
        TON = 0,
        EVM = 1,
    }

    [MessagePackObject]
    public record RoomBetDetails([property: Key(0)] string BetValueRaw, [property: Key(1)] RoomCoin Coin, [property: Key(2)] RollingBet? RollingBet)
    {
        [IgnoreMember]
        public decimal BetValue => RawCoinConverter.FromRaw(BetValueRaw, Coin.Currency.Decimals);

        public virtual bool Equals(RoomBetDetails? other) => other != null && BetValueRaw == other.BetValueRaw && Coin.Equals(other.Coin);

        public override int GetHashCode() => HashCode.Combine(BetValue, Coin);

        public override string ToString() => $"${nameof(BetValue)}:{BetValue}{Environment.NewLine}"
            + $"{nameof(Coin)}: Id: {Coin.CoinId} | Ticker: {Coin.Currency.Ticker} | ChainType: {Coin.Chain.Type}";
    }

    [MessagePackObject]
    public class RoomCoin
    {
        [Key(0)] public Guid CoinId { get; set; }
        [Key(1)] public RoomChain Chain { get; set; }
        [Key(2)] public RoomCurrency Currency { get; set; }

        private bool Equals(RoomCoin other) => CoinId.Equals(other.CoinId) && Chain.Equals(other.Chain) && Currency.Equals(other.Currency);
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is RoomCoin other && Equals(other));

        public override int GetHashCode() => HashCode.Combine(CoinId, Chain, Currency);

        public static bool operator ==(RoomCoin? left, RoomCoin? right) => Equals(left, right);

        public static bool operator !=(RoomCoin? left, RoomCoin? right) => !Equals(left, right);
    }

    [MessagePackObject]
    public class RoomChain
    {
        [Key(0)] public int ExternalId { get; set; }
        [Key(1)] public ChainType Type { get; set; }
        [Key(2)] public string Name { get; set; }

        private bool Equals(RoomChain other) => ExternalId == other.ExternalId && Type == other.Type && Name == other.Name;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is RoomChain other && Equals(other));

        public override int GetHashCode() => HashCode.Combine(ExternalId, (int)Type, Name);

        public static bool operator ==(RoomChain? left, RoomChain? right) => Equals(left, right);

        public static bool operator !=(RoomChain? left, RoomChain? right) => !Equals(left, right);
    }

    [MessagePackObject]
    public class RoomCurrency
    {
        [Key(0)] public string Ticker { get; set; } = null!;
        [Key(1)] public string? Address { get; set; }
        [Key(2)] public int Decimals { get; set; }
        [Key(3)] public string IconUrl { get; set; } = null!;

        private bool Equals(RoomCurrency other) => Ticker == other.Ticker && Address == other.Address && Decimals == other.Decimals && IconUrl == other.IconUrl;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is RoomCurrency other && Equals(other));

        public override int GetHashCode() => HashCode.Combine(Ticker, Address, Decimals, IconUrl);

        public static bool operator ==(RoomCurrency? left, RoomCurrency? right) => Equals(left, right);

        public static bool operator !=(RoomCurrency? left, RoomCurrency? right) => !Equals(left, right);
    }

    [MessagePackObject]
    public record RollingBet([property: Key(0)] Guid RollingBetId, [property: Key(1)] int? NumberOfPlayers, [property: Key(2)] string EntryFee, [property: Key(3)] string Prize)
    {
        public virtual bool Equals(RollingBet? other) => RollingBetId == other?.RollingBetId;
        public override int GetHashCode() => RollingBetId.GetHashCode();
    }
}
