#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics.Models.Matchmaking;

namespace Elympics.ElympicsSystems.Internal
{
    internal abstract class ElympicsLobbyClientState
    {
        protected readonly ElympicsLobbyClient Client;

        public ElympicsState State;
        private const string WarningMessageFormat = "{0} during {1} state";
        private const string ErrorMessageFormat = "Must not {0} during {1} state";
        protected CancellationTokenSource _cts;

        protected ElympicsLobbyClientState(ElympicsLobbyClient client) => Client = client;

        public abstract UniTask Connect(ConnectionData data);
        public abstract UniTask ReConnect(ConnectionData reconnectionData);
        public abstract UniTask SignOut();

        public abstract UniTask Disconnect();
        public abstract UniTask StartMatchmaking(IRoom room);
        public abstract UniTask CancelMatchmaking(IRoom room, CancellationToken ct = default);
        public abstract UniTask PlayMatch(MatchmakingFinishedData matchData);
        public abstract UniTask WatchReplay();
        public abstract UniTask FinishMatch();
        public abstract void MatchFound();

        protected string GenerateErrorMessage(string action) => string.Format(ErrorMessageFormat, action, State);
        protected string GenerateWarningMessage(string action) => string.Format(WarningMessageFormat, action, State);
    }
}
