using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record MatchDetails(
        [property: Key(0)] IReadOnlyList<Guid> MatchedPlayersId,
        [property: Key(1)] string TcpUdpServerAddress,
        [property: Key(2)] string WebServerAddress,
        [property: Key(3)] string UserSecret,
        [property: Key(4)] byte[] GameEngineData,
        [property: Key(5)] float[] MatchmakerData)
    {
        public virtual bool Equals(MatchDetails? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return CompareIEnumerable(MatchedPlayersId, other.MatchedPlayersId)
                && TcpUdpServerAddress == other.TcpUdpServerAddress
                && WebServerAddress == other.WebServerAddress
                && UserSecret == other.UserSecret
                && CompareIEnumerable(GameEngineData, other.GameEngineData)
                && CompareIEnumerable(MatchmakerData, other.MatchmakerData);
        }

        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        public override string ToString() => $"{nameof(MatchedPlayersId)}:{Environment.NewLine}\t{string.Join(Environment.NewLine + "\t", MatchedPlayersId ?? new List<Guid>())}{Environment.NewLine}"
            + $"{nameof(TcpUdpServerAddress)}:{TcpUdpServerAddress}{Environment.NewLine}"
            + $"{nameof(WebServerAddress)}:{WebServerAddress}{Environment.NewLine}"
            + $"{nameof(GameEngineData)}:{GameEngineData?.Length}{Environment.NewLine}"
            + $"{nameof(MatchmakerData)}:{MatchmakerData?.Length}{Environment.NewLine}";

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(MatchedPlayersId.Count);
            hashCode.Add(TcpUdpServerAddress);
            hashCode.Add(WebServerAddress);
            hashCode.Add(UserSecret);
            hashCode.Add(GameEngineData.Length);
            hashCode.Add(MatchmakerData.Length);
            return hashCode.ToHashCode();
        }

        private static bool CompareIEnumerable<TSource>(IEnumerable<TSource>? first, IEnumerable<TSource>? second)
        {
            if (first == null && second == null)
                return true;
            if (first == null || second == null)
                return false;
            return first.SequenceEqual(second);
        }
    }
}
