#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Elympics.Communication.Mappers
{
    internal static class RollingTournamentBetConfigIDs
    {
        private static readonly Dictionary<BetConfig, Guid> BetConfigToId = new();

        internal static async UniTask<Guid> GetConfigId(Guid coinId, decimal prize, int numberOfPlayers, decimal[] prizeDistribution, CancellationToken ct = default)
        {
            var betConfig = new BetConfig(coinId, prize, numberOfPlayers);

            if (BetConfigToId.TryGetValue(betConfig, out var id))
                return id;

            var lobbyClient = ElympicsLobbyClient.Instance ?? throw new ArgumentNullException(nameof(ElympicsLobbyClient));
            var coinInfo = lobbyClient.AvailableCoins?.FirstOrDefault(coin => coin.Id == coinId);

            if (!coinInfo.HasValue || coinInfo.Value.Id == Guid.Empty)
                throw new ArgumentException($"Coin info for coin with ID {coinId} not found.", nameof(coinId));

            var payload = new[] { new TournamentFeeRequestInfo { CoinInfo = coinInfo.Value, Prize = prize, PlayersCount = numberOfPlayers, PrizeDistribution = prizeDistribution } };
            var response = await lobbyClient.GetRollTournamentsFeeInternal(payload, ct) ?? throw new ElympicsException("Failed to get rolling tournament bet config ID.");

            var rollingTournamentBetConfigId = response.Rollings[0].RollingTournamentBetConfigId;
            BetConfigToId[betConfig] = rollingTournamentBetConfigId;

            return rollingTournamentBetConfigId;
        }

        internal static void AddOrUpdate(Guid coinId, decimal prize, int numberOfPlayers, Guid rollingTournamentBetConfigId) => BetConfigToId[new BetConfig(coinId, prize, numberOfPlayers)] = rollingTournamentBetConfigId;

        private readonly struct BetConfig : IEquatable<BetConfig>
        {
            private readonly int _numberOfPlayers;
            private readonly decimal _prize;
            private readonly Guid _coinId;

            public bool Equals(BetConfig other) => _coinId.Equals(other._coinId) && _prize == other._prize && _numberOfPlayers == other._numberOfPlayers;

            public override bool Equals(object? obj) => obj is BetConfig other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(_coinId, _prize, _numberOfPlayers);

            public static bool operator ==(BetConfig left, BetConfig right) => left.Equals(right);

            public static bool operator !=(BetConfig left, BetConfig right) => !left.Equals(right);

            public BetConfig(Guid coinId, decimal prize, int numberOfPlayers)
            {
                _coinId = coinId;
                _prize = prize;
                _numberOfPlayers = numberOfPlayers;
            }
        }
    }
}
