#nullable enable

using System;

namespace Elympics.Communication.Rooms.PublicModels
{
    public class TournamentDetails
    {
        public readonly string TournamentId;
        public readonly TournamentType TournamentType;

        public static TournamentDetails Regular(string id) => new(id, TournamentType.Regular);
        public static TournamentDetails Rolling(Guid id) => new(id.ToString(), TournamentType.Rolling);

        private TournamentDetails(string tournamentId, TournamentType tournamentType)
        {
            TournamentId = tournamentId ?? throw new ArgumentNullException(nameof(tournamentId));
            TournamentType = tournamentType;
        }
    }

    public enum TournamentType
    {
        Regular,
        Rolling
    }
}
