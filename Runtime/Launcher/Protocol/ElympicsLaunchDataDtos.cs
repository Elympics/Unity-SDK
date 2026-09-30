#nullable enable

using System;

namespace Elympics
{
    // Wire forms of the launch data, one flat JsonUtility DTO per match mode, plus the shared options.
    // JsonUtility has no nullables, so "unset" is an empty string or array, 0 for ports and counts, and -1 for indices.

    /// <summary>The <c>options</c> argument of every launch command. Empty JSON means all defaults.</summary>
    [Serializable]
    internal class ElympicsLaunchOptionsDto
    {
        public string requestId = "";
        /// <summary>0 means <see cref="ElympicsLaunchDefaults.LaunchTimeoutSeconds"/>, -1 means never.</summary>
        public int launchTimeoutSeconds;
        /// <summary>0 means <see cref="ElympicsLaunchDefaults.PostMatchTimeoutSeconds"/>, -1 means never.</summary>
        public int postMatchTimeoutSeconds;

        public bool TryToOptions(out ElympicsLaunchOptions? options, out ElympicsLaunchFailure? failure)
        {
            options = null;
            if (!TryToTimeout(launchTimeoutSeconds, nameof(launchTimeoutSeconds), out var launchTimeout, out failure)
                || !TryToTimeout(postMatchTimeoutSeconds, nameof(postMatchTimeoutSeconds), out var postMatchTimeout, out failure))
                return false;
            options = new ElympicsLaunchOptions
            {
                RequestId = string.IsNullOrEmpty(requestId) ? null : requestId,
                LaunchTimeout = launchTimeout,
                PostMatchTimeout = postMatchTimeout,
            };
            return true;
        }

        private static bool TryToTimeout(int seconds, string name, out TimeSpan? timeout, out ElympicsLaunchFailure? failure)
        {
            failure = null;
            timeout = seconds switch
            {
                0 => null,
                ElympicsLaunchDefaults.NoTimeout => System.Threading.Timeout.InfiniteTimeSpan,
                _ => TimeSpan.FromSeconds(seconds),
            };
            if (seconds >= ElympicsLaunchDefaults.NoTimeout)
                return true;
            failure = new ElympicsLaunchFailure(ElympicsLaunchErrorCode.InvalidPayload,
                $"{name} must be positive, 0 (default) or {ElympicsLaunchDefaults.NoTimeout} (never), got {seconds}.");
            return false;
        }
    }

    internal static class ElympicsLaunchDtoParsing
    {
        public const string TransportWebRtc = "webRtc";
        public const string TransportTcpUdp = "tcpUdp";

        public static bool TryParseTransport(string? value, out bool? useTcpUdp, out ElympicsLaunchFailure? failure)
        {
            failure = null;
            useTcpUdp = null;
            switch (value)
            {
                case null or "":
                    return true;
                case TransportWebRtc:
                    useTcpUdp = false;
                    return true;
                case TransportTcpUdp:
                    useTcpUdp = true;
                    return true;
                default:
                    return Fail($"transport must be \"\", \"{TransportWebRtc}\" or \"{TransportTcpUdp}\", got \"{value}\".", out failure);
            }
        }

        public static bool TryParseGuid(string? value, string name, bool required, out Guid? guid, out ElympicsLaunchFailure? failure)
        {
            failure = null;
            guid = null;
            if (string.IsNullOrEmpty(value))
                return !required || Fail($"{name} is required.", out failure);
            if (Guid.TryParse(value, out var parsed))
            {
                guid = parsed;
                return true;
            }
            return Fail($"{name} must be a GUID, got \"{value}\".", out failure);
        }

        public static bool TryParseGuids(string[]? values, string name, out Guid[] guids, out ElympicsLaunchFailure? failure)
        {
            failure = null;
            values ??= Array.Empty<string>();
            guids = new Guid[values.Length];
            for (var i = 0; i < values.Length; i++)
                if (!Guid.TryParse(values[i], out guids[i]))
                    return Fail($"{name}[{i}] must be a GUID, got \"{values[i]}\".", out failure);
            return true;
        }

        public static bool TryParseBase64(string? value, string name, out byte[]? bytes, out ElympicsLaunchFailure? failure)
        {
            failure = null;
            bytes = null;
            if (string.IsNullOrEmpty(value))
                return true;
            try
            {
                bytes = Convert.FromBase64String(value);
                return true;
            }
            catch (FormatException)
            {
                return Fail($"{name} must be base64.", out failure);
            }
        }

        public static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

        public static float[]? NullIfEmpty(float[]? values) => values is { Length: > 0 } ? values : null;

        private static bool Fail(string message, out ElympicsLaunchFailure? failure)
        {
            failure = new ElympicsLaunchFailure(ElympicsLaunchErrorCode.InvalidPayload, message);
            return false;
        }
    }

    /// <summary>Wire form of <see cref="LaunchData"/>, parsed from the <c>data</c> argument of a launch command.</summary>
    internal interface ILaunchDataDto
    {
        bool TryToLaunchData(out LaunchData? data, out ElympicsLaunchFailure? failure);
    }

    [Serializable]
    internal class OnlineLaunchDataDto : ILaunchDataDto
    {
        public string serverAddress = "";
        /// <summary>"" (use the game config), "webRtc" or "tcpUdp".</summary>
        public string transport = "";
        public string matchId = "";
        public string userSecret = "";
        public string userId = "";
        public string[] matchedPlayers = Array.Empty<string>();
        public string regionName = "";
        /// <summary>Base64-encoded bytes.</summary>
        public string gameEngineData = "";
        public float[] matchmakerData = Array.Empty<float>();

        public bool TryToLaunchData(out LaunchData? data, out ElympicsLaunchFailure? failure)
        {
            data = null;
            if (!ElympicsLaunchDtoParsing.TryParseTransport(transport, out var useTcpUdp, out failure)
                || !ElympicsLaunchDtoParsing.TryParseGuid(matchId, nameof(matchId), true, out var parsedMatchId, out failure)
                || !ElympicsLaunchDtoParsing.TryParseGuid(userId, nameof(userId), true, out var parsedUserId, out failure)
                || !ElympicsLaunchDtoParsing.TryParseGuids(matchedPlayers, nameof(matchedPlayers), out var players, out failure)
                || !ElympicsLaunchDtoParsing.TryParseBase64(gameEngineData, nameof(gameEngineData), out var engineData, out failure))
                return false;
            data = new OnlineLaunchData(serverAddress, useTcpUdp, parsedMatchId!.Value, userSecret, parsedUserId!.Value, players,
                ElympicsLaunchDtoParsing.NullIfEmpty(regionName), engineData, matchmakerData);
            return true;
        }
    }

    [Serializable]
    internal class SinglePlayerLaunchDataDto : ILaunchDataDto
    {
        public string matchId = "";
        public string userId = "";
        public string queueName = "";
        public string regionName = "";
        /// <summary>Base64-encoded bytes.</summary>
        public string gameEngineData = "";
        public float[] matchmakerData = Array.Empty<float>();

        public bool TryToLaunchData(out LaunchData? data, out ElympicsLaunchFailure? failure)
        {
            data = null;
            if (!ElympicsLaunchDtoParsing.TryParseGuid(matchId, nameof(matchId), false, out var parsedMatchId, out failure)
                || !ElympicsLaunchDtoParsing.TryParseGuid(userId, nameof(userId), false, out var parsedUserId, out failure)
                || !ElympicsLaunchDtoParsing.TryParseBase64(gameEngineData, nameof(gameEngineData), out var engineData, out failure))
                return false;
            data = new SinglePlayerLaunchData(parsedMatchId, parsedUserId, ElympicsLaunchDtoParsing.NullIfEmpty(queueName),
                ElympicsLaunchDtoParsing.NullIfEmpty(regionName), engineData, matchmakerData);
            return true;
        }
    }

    [Serializable]
    internal class HalfRemoteClientLaunchDataDto : ILaunchDataDto
    {
        /// <summary><c>ip:port</c>, or "" for the game config.</summary>
        public string serverAddress = "";
        /// <summary>"" (use the game config), "webRtc" or "tcpUdp".</summary>
        public string transport = "";
        /// <summary>-1 means the game config.</summary>
        public int playerIndex = -1;
        public string regionName = "";
        public string queueName = "";
        /// <summary>Base64-encoded bytes.</summary>
        public string gameEngineData = "";
        public float[] matchmakerData = Array.Empty<float>();

        public bool TryToLaunchData(out LaunchData? data, out ElympicsLaunchFailure? failure)
        {
            data = null;
            if (!ElympicsLaunchDtoParsing.TryParseTransport(transport, out var useTcpUdp, out failure)
                || !ElympicsLaunchDtoParsing.TryParseBase64(gameEngineData, nameof(gameEngineData), out var engineData, out failure))
                return false;
            if (playerIndex < -1)
            {
                failure = new ElympicsLaunchFailure(ElympicsLaunchErrorCode.InvalidPayload, $"{nameof(playerIndex)} must be -1 (default) or an index, got {playerIndex}.");
                return false;
            }
            data = new HalfRemoteClientLaunchData(ElympicsLaunchDtoParsing.NullIfEmpty(serverAddress), useTcpUdp, playerIndex < 0 ? null : playerIndex,
                ElympicsLaunchDtoParsing.NullIfEmpty(regionName), ElympicsLaunchDtoParsing.NullIfEmpty(queueName), engineData,
                ElympicsLaunchDtoParsing.NullIfEmpty(matchmakerData));
            return true;
        }
    }

    [Serializable]
    internal class HalfRemoteServerLaunchDataDto : ILaunchDataDto
    {
        public const string ProtocolsTcpUdp = "tcpUdp";
        public const string ProtocolsWebRtc = "webRtc";
        public const string ProtocolsBoth = "both";

        /// <summary>"" (both), "tcpUdp", "webRtc" or "both".</summary>
        public string protocols = "";
        /// <summary>IP address to listen on, or "" for the game config.</summary>
        public string serverHostname = "";
        /// <summary>0 means the game config.</summary>
        public int tcpUdpPort;
        /// <summary>0 means the game config.</summary>
        public int signalingPort;
        /// <summary>0 means all test players.</summary>
        public int maxPlayers;

        public bool TryToLaunchData(out LaunchData? data, out ElympicsLaunchFailure? failure)
        {
            data = null;
            failure = null;
            ElympicsHalfRemoteProtocols? parsedProtocols;
            switch (protocols)
            {
                case null or "":
                    parsedProtocols = null;
                    break;
                case ProtocolsTcpUdp:
                    parsedProtocols = ElympicsHalfRemoteProtocols.TcpUdp;
                    break;
                case ProtocolsWebRtc:
                    parsedProtocols = ElympicsHalfRemoteProtocols.WebRtc;
                    break;
                case ProtocolsBoth:
                    parsedProtocols = ElympicsHalfRemoteProtocols.Both;
                    break;
                default:
                    failure = new ElympicsLaunchFailure(ElympicsLaunchErrorCode.InvalidPayload,
                        $"{nameof(protocols)} must be \"\", \"{ProtocolsTcpUdp}\", \"{ProtocolsWebRtc}\" or \"{ProtocolsBoth}\", got \"{protocols}\".");
                    return false;
            }
            data = new HalfRemoteServerLaunchData(parsedProtocols, ElympicsLaunchDtoParsing.NullIfEmpty(serverHostname),
                tcpUdpPort == 0 ? null : tcpUdpPort, signalingPort == 0 ? null : signalingPort, maxPlayers == 0 ? null : maxPlayers);
            return true;
        }
    }

    [Serializable]
    internal class SnapshotReplayLaunchDataDto : ILaunchDataDto
    {
        /// <summary>An http(s) URL, a replay file, or a directory ending with a separator.</summary>
        public string path = "";

        public bool TryToLaunchData(out LaunchData? data, out ElympicsLaunchFailure? failure)
        {
            failure = null;
            data = new SnapshotReplayLaunchData(path);
            return true;
        }
    }

    internal static class ElympicsLaunchDtos
    {
        /// <summary>A default instance of the DTO of <paramref name="kind"/>, e.g. for templates.</summary>
        public static ILaunchDataDto Create(ElympicsHostLaunchKind kind) => kind switch
        {
            ElympicsHostLaunchKind.Online => new OnlineLaunchDataDto(),
            ElympicsHostLaunchKind.SinglePlayer => new SinglePlayerLaunchDataDto(),
            ElympicsHostLaunchKind.HalfRemoteClient => new HalfRemoteClientLaunchDataDto(),
            ElympicsHostLaunchKind.HalfRemoteServer => new HalfRemoteServerLaunchDataDto(),
            ElympicsHostLaunchKind.SnapshotReplay => new SnapshotReplayLaunchDataDto(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }
}
