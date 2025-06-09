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

        [PublicAPI]
        public static CompetitivenessConfig Regular(string id) => new(id, CompetitivenessType.Tournament, 0);
        [PublicAPI]
        public static CompetitivenessConfig Rolling(Guid id) => new(id.ToString(), CompetitivenessType.RollingTournament, 0);
        [PublicAPI]
        public static CompetitivenessConfig Bet(Guid coinId, decimal amount) => new(coinId.ToString(), CompetitivenessType.RollingTournament, amount);

        private CompetitivenessConfig(string tournamentId, CompetitivenessType competitivenessType, decimal value)
        {
            ID = tournamentId ?? throw new ArgumentNullException(nameof(tournamentId));
            CompetitivenessType = competitivenessType;
            Value = value;
        }
    }

    public enum CompetitivenessType
    {
        Tournament,
        RollingTournament,
        Bet
    }
}
