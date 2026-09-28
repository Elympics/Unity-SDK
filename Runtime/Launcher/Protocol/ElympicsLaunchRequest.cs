#nullable enable

using System;
using System.Linq;
using Elympics.ElympicsSystems.Internal;
using Elympics.Models.Matchmaking;
using Elympics.SnapshotAnalysis.Retrievers;
using JetBrains.Annotations;

namespace Elympics
{
    /// <summary>
    /// Describes a match to be started by <c>IElympicsMatchLauncher.LaunchMatchAsync</c>.
    /// Flat and <see cref="UnityEngine.JsonUtility"/>-compatible, so the same type is used over the wire.
    /// </summary>
    /// <remarks>
    /// <see cref="userSecret"/> is a per-match game-server credential, unrelated to any account-level Elympics
    /// client secret. No bearer token is ever part of a launch request.
    /// </remarks>
    [Serializable, PublicAPI]
    public class ElympicsLaunchRequest
    {
        public string protocolVersion = ElympicsHostProtocol.Version;
        /// <summary>Echoed in every response to this request. Generated if empty.</summary>
        public string requestId = "";

        /// <summary>GUID of an <see cref="ElympicsGameConfig"/> in <see cref="ElympicsConfig.AvailableGames"/>.</summary>
        public string gameId = "";
        /// <summary>Name of a <see cref="JoinedMatchMode"/> member: Online, SinglePlayer, HalfRemoteClient, HalfRemoteServer or SnapshotReplay.</summary>
        public string matchMode = "";
        /// <summary>GUID of the local player. Required for Online, optional for SinglePlayer.</summary>
        public string userId = "";

        // Match data (see MatchmakingFinishedData). Read only by Online and SinglePlayer modes.
        public string matchId = "";
        public string userSecret = "";
        /// <summary>Read by SinglePlayer only.</summary>
        public string queueName = "";
        public string regionName = "";
        public string tcpUdpServerAddress = "";
        public string webServerAddress = "";
        /// <summary>Base64-encoded bytes.</summary>
        public string gameEngineData = "";
        public float[] matchmakerData = Array.Empty<float>();
        /// <summary>GUIDs of all players. Their order defines <see cref="ElympicsPlayer"/> indices.</summary>
        public string[] matchedPlayers = Array.Empty<string>();

        /// <summary>0 means <see cref="ElympicsLaunchDefaults.LaunchTimeoutSeconds"/>.</summary>
        public int launchTimeoutSeconds;
        /// <summary>0 means <see cref="ElympicsLaunchDefaults.PostMatchTimeoutSeconds"/>, -1 means never.</summary>
        public int postMatchTimeoutSeconds;

        /// <summary>Source of a SnapshotReplay match. In-process only: cannot be sent over the wire.</summary>
        internal SnapshotAnalysisRetriever? SnapshotRetriever { get; set; }

        /// <summary>Builds a request for a match described by <paramref name="matchData"/>, e.g. received from matchmaking.</summary>
        public static ElympicsLaunchRequest FromMatchData(string gameId, JoinedMatchMode matchMode, Guid userId, MatchmakingFinishedData matchData) => new()
        {
            gameId = gameId,
            matchMode = matchMode.ToString(),
            userId = userId.ToString(),
            matchId = matchData.MatchId.ToString(),
            userSecret = matchData.UserSecret ?? "",
            queueName = matchData.QueueName ?? "",
            regionName = matchData.RegionName ?? "",
            tcpUdpServerAddress = matchData.TcpUdpServerAddress ?? "",
            webServerAddress = matchData.WebServerAddress ?? "",
            gameEngineData = matchData.GameEngineData is { Length: > 0 } bytes ? Convert.ToBase64String(bytes) : "",
            matchmakerData = matchData.MatchmakerData ?? Array.Empty<float>(),
            matchedPlayers = matchData.MatchedPlayers?.Select(x => x.ToString()).ToArray() ?? Array.Empty<string>(),
        };
    }
}
