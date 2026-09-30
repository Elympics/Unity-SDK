#nullable enable

using System;
using JetBrains.Annotations;

// Wire payloads of the host protocol. Public fields with lower-case names, as required by JsonUtility.
namespace Elympics
{
    [Serializable, PublicAPI]
    public class ElympicsReadyInfo
    {
        public string sdkVersion = "";
        public string protocolVersion = "";
        public string[] gameIds = Array.Empty<string>();
        /// <summary>Configuration problems the host should know about, e.g. an unusable boot scene. Empty if there are none.</summary>
        public string[] warnings = Array.Empty<string>();
    }

    [Serializable, PublicAPI]
    public class ElympicsLaunchFailure
    {
        /// <summary>Name of an <see cref="ElympicsLaunchErrorCode"/> member.</summary>
        public string code = "";
        /// <summary>Human-readable only. Do not parse.</summary>
        public string message = "";

        public ElympicsLaunchFailure()
        { }

        public ElympicsLaunchFailure(ElympicsLaunchErrorCode code, string message)
        {
            this.code = code.ToString();
            this.message = message;
        }

        public ElympicsLaunchErrorCode Code =>
            Enum.TryParse<ElympicsLaunchErrorCode>(code, out var parsed) ? parsed : ElympicsLaunchErrorCode.InternalError;

        public override string ToString() => $"{code}: {message}";
    }

    [Serializable, PublicAPI]
    public class ElympicsLaunchSucceededInfo
    {
        public string gameId = "";
        public string matchId = "";
    }

    [Serializable, PublicAPI]
    public class ElympicsMatchEndedInfo
    {
        public string matchId = "";
    }

    [Serializable, PublicAPI]
    public class ElympicsDisconnectedInfo
    {
        public string matchId = "";
        public bool byServer;
    }

    [Serializable, PublicAPI]
    public class ElympicsQuitInfo
    {
        public string reason = "";
    }
}
