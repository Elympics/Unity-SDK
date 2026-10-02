#nullable enable

using System;

namespace Elympics
{
    internal interface IElympicsMatchLifecycleSink
    {
        void SceneInitializationFailed(ElympicsLaunchErrorCode code, string message);
        void MatchJoined();
        void JoinFailed(string message);
        void MatchEnded(Guid matchId);
        void Disconnected(bool byServer);
    }
}
