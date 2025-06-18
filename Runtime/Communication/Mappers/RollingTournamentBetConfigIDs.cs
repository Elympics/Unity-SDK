#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Communication.Rooms.PublicModels;

namespace Elympics.Communication.Mappers
{
    internal static class RollingTournamentBetConfigIDs
    {
        private static readonly Dictionary<BetConfig, Guid> BetConfigToId = new();

        public static async UniTask<Guid> GetConfigId(CompetitivenessConfig config, CancellationToken ct = default)
        {
            var coinId = Guid.Parse(config.ID);
            var betConfig = new BetConfig(coinId, config.Value, config.NumberOfPlayers);

            if (BetConfigToId.TryGetValue(betConfig, out var id))
                return id;

            var lobbyClient = ElympicsLobbyClient.Instance ?? throw new ArgumentNullException(nameof(ElympicsLobbyClient));
            var coinInfo = lobbyClient.AvailableCoins?.FirstOrDefault(coin => coin.Id == coinId);

            if (!coinInfo.HasValue || coinInfo.Value.Id == Guid.Empty)
                throw new ArgumentException($"Coin info for coin with ID {betConfig.CoinId} not found.", nameof(config));

            var response = await lobbyClient.GetRollTournamentsFeeInternal(new[] { new TournamentFeeRequestInfo { CoinInfo = coinInfo.Value , Prize = betConfig.Prize, PlayersCount = betConfig.NumberOfPlayers } }, ct);

            if (response == null)
                throw new ElympicsException("Failed to get rolling tournament bet config ID.");

            var rollingTournamentBetConfigId = response.Rollings[0].RollingTournamentBetConfigId;
            BetConfigToId[betConfig] = rollingTournamentBetConfigId;

            return rollingTournamentBetConfigId;
        }

        private readonly struct BetConfig : IEquatable<BetConfig>
        {
            public readonly int NumberOfPlayers;
            public readonly decimal Prize;
            public readonly Guid CoinId;

            public bool Equals(BetConfig other) => CoinId.Equals(other.CoinId) && Prize == other.Prize && NumberOfPlayers == other.NumberOfPlayers;

            public override bool Equals(object? obj) => obj is BetConfig other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(CoinId, Prize, NumberOfPlayers);

            public static bool operator ==(BetConfig left, BetConfig right) => left.Equals(right);

            public static bool operator !=(BetConfig left, BetConfig right) => !left.Equals(right);

            public BetConfig(Guid coinId, decimal prize, int numberOfPlayers)
            {
                CoinId = coinId;
                Prize = prize;
                NumberOfPlayers = numberOfPlayers;
            }
        }
    }
}
