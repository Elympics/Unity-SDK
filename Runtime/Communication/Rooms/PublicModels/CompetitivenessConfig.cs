#nullable enable

using System;
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

        [PublicAPI]
        public static CompetitivenessConfig GlobalTournament(string id) => new(id, CompetitivenessType.GlobalTournament, 0, -1);

        [PublicAPI]
        public static CompetitivenessConfig RollingTournament(int numberOfPlayers, decimal prize, Guid coinId)
        {
            if (numberOfPlayers <= 0)
                throw new ArgumentOutOfRangeException(nameof(numberOfPlayers), numberOfPlayers, "The number of players must be greater than zero.");

            return new CompetitivenessConfig(coinId.ToString(), CompetitivenessType.RollingTournament, prize, numberOfPlayers);
        }

        [PublicAPI]
        public static CompetitivenessConfig Bet(Guid coinId, decimal amount) => new(coinId.ToString(), CompetitivenessType.Bet, amount, -1);

        private CompetitivenessConfig(string tournamentId, CompetitivenessType competitivenessType, decimal value, int numberOfPlayers)
        {
            ID = tournamentId ?? throw new ArgumentNullException(nameof(tournamentId));
            CompetitivenessType = competitivenessType;
            Value = value;
            NumberOfPlayers = numberOfPlayers;
        }
    }

    public enum CompetitivenessType
    {
        GlobalTournament,
        RollingTournament,
        Bet
    }
}
