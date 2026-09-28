#nullable enable

using System;

namespace Elympics
{
    /// <summary>Reports from the gameplay scene back to the match launcher.</summary>
    internal interface IElympicsMatchLifecycleSink
    {
        void SceneInitializationFailed(ElympicsLaunchErrorCode code, string message);
        void MatchJoined();
        void JoinFailed(string message);
        void MatchEnded(Guid matchId);
        void Disconnected(bool byServer);
    }
}
