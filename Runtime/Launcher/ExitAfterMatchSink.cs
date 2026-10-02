#nullable enable

using System;
using Elympics.Core.Logger;

namespace Elympics
{
    internal sealed class ExitAfterMatchSink : IElympicsMatchLifecycleSink
    {
        private readonly Action<string> _exit;
        private bool _exited;

        public ExitAfterMatchSink(Action<string> exit) => _exit = exit;

        public void SceneInitializationFailed(ElympicsLaunchErrorCode code, string message) => Exit($"The match could not start: {code}: {message}", true);
        public void MatchJoined()
        { }
        public void JoinFailed(string message) => Exit($"Joining the match failed: {message}", true);
        public void MatchEnded(Guid matchId) => Exit($"Match {matchId} ended.", false);
        public void Disconnected(bool byServer) => Exit($"Disconnected by {(byServer ? "server" : "client")}.", true);

        private void Exit(string reason, bool isError)
        {
            if (_exited)
                return;
            _exited = true;
            var message = $"{reason} This process runs only the match set by ELYMPICS_LAUNCH_MODE, so it quits.";
            if (isError)
                ElympicsLogger.LogError(message);
            else
                ElympicsLogger.LogInfo(message);
            _exit(reason);
        }
    }
}
