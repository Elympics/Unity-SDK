#nullable enable

using System;
using System.Linq;
using Elympics.Core.Utils;
using JetBrains.Annotations;

namespace Elympics.Communication.Rooms.PublicModels
{
    public class CompetitivenessConfig
    {
        /// <summary>Type of competitiveness selected. Determines how other fields should be interpreted.</summary>
        internal readonly CompetitivenessType CompetitivenessType;
        /// <summary>Tournament ID, rolling tournament type ID or coin ID.</summary>
        internal readonly string ID;
        /// <summary>Bet amount. Only used with <see cref="CompetitivenessType.Bet"/>.</summary>
        internal readonly decimal Value;
        /// <summary>Only used with <see cref="CompetitivenessType.RollingTournament"/>.</summary>
        internal readonly int NumberOfPlayers;
        /// <summary>Only used with <see cref="CompetitivenessType.RollingTournament"/>.</summary>
        internal readonly decimal[] PrizeDistribution;

        [PublicAPI]
        public static CompetitivenessConfig GlobalTournament(string id) => new(id, CompetitivenessType.GlobalTournament, 0, -1, Array.Empty<decimal>());

        /// <summary>Creates a configuration for a rolling tournament.</summary>
        /// <param name="numberOfPlayers">Number of players who must join the tournament and play a match in it in order for the tournament to be finished.</param>
        /// <param name="totalPrize">Prize distributed among the top players according to the values from <paramref name="prizeDistribution"/>.</param>
        /// <param name="coinId">ID of the coin to use.</param>
        /// <param name="prizeDistribution">
        /// Determines how <paramref name="totalPrize"/> will be distributed among the top players.
        /// Numbers in this array should add up to ~1.
        /// When the tournament ends, <paramref name="totalPrize"/> will be multiplied by the value from this array for
        /// each place to calculate the reward for the player on that place on the final leaderboard.
        /// If <see cref="Array.Length"/> of this array is less than <paramref name="numberOfPlayers"/>, rewards for the remaining places will default to 0.
        /// If a null is passed, or the array is empty, the top scoring player will receive the full <paramref name="totalPrize"/>.
        /// </param>
        [PublicAPI]
        public static CompetitivenessConfig RollingTournament(int numberOfPlayers, decimal totalPrize, Guid coinId, decimal[]? prizeDistribution = null)
        {
            const decimal tolerance = 0.01m;

            if (numberOfPlayers <= 1)
                throw new ArgumentOutOfRangeException(nameof(numberOfPlayers), numberOfPlayers, "The number of players must be greater than one.");
            if (prizeDistribution != null && numberOfPlayers < prizeDistribution.Length)
                throw new ArgumentException($"{nameof(prizeDistribution)} can't include more places on the leaderboard than there are players in the tournament. {nameof(numberOfPlayers)}: {numberOfPlayers} {nameof(prizeDistribution)}.{nameof(prizeDistribution.Length)}: {prizeDistribution.Length}.", nameof(prizeDistribution));
            if (prizeDistribution != null && Math.Abs(prizeDistribution.Sum() - 1m) > tolerance)
                throw new ArgumentException($"{nameof(prizeDistribution)} values have to sum up to 1 +/-{tolerance}. Sum: {prizeDistribution.Sum()} Values: {prizeDistribution.CommaList()}.", nameof(prizeDistribution));

            return new CompetitivenessConfig(coinId.ToString(), CompetitivenessType.RollingTournament, totalPrize, numberOfPlayers, prizeDistribution ?? Array.Empty<decimal>());
        }

        /// <summary>Creates a configuration for a competitive match with a bet.</summary>
        /// <param name="coinId">ID of the coin to use.</param>
        /// <param name="amount">The amount that the player will be charged as a match entry fee.</param>
        /// <remarks>The prize received by each player on the winning team will be equal to <paramref name="amount"/> minus commission.</remarks>
        [PublicAPI]
        public static CompetitivenessConfig Bet(Guid coinId, decimal amount) => new(coinId.ToString(), CompetitivenessType.Bet, amount, -1, Array.Empty<decimal>());

        private CompetitivenessConfig(string tournamentId, CompetitivenessType competitivenessType, decimal value, int numberOfPlayers, decimal[] prizeDistribution)
        {
            ID = tournamentId ?? throw new ArgumentNullException(nameof(tournamentId));
            CompetitivenessType = competitivenessType;
            Value = value;
            NumberOfPlayers = numberOfPlayers;
            PrizeDistribution = prizeDistribution;
        }
    }
}
