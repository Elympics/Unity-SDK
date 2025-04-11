using System;
using JetBrains.Annotations;

namespace Elympics.ElympicsSystems.Internal
{
    [PublicAPI]
    internal struct ElympicsLoggerContext
    {
        public const string ElympicsContextApp = "ElympicsSdk";
        public const string GameplayContextApp = "ElympicsGame";
        public const string PlayPadContextApp = "PlayPadSdk";
        public Guid SessionId;
        public string App;
        public ElympicsContext ElympicsContext;
        public UserContext UserContext;
        public ConnectionContext ConnectionContext;
        public RoomContext RoomContext;
        public PlayPadContext PlayPadContext;
        public string Context;
        public string MethodName;

        public ElympicsLoggerContext(Guid sessionId)
        {
            App = null;
            SessionId = sessionId;
            ElympicsContext = new ElympicsContext
            {
                SdkVersion = null,
                GameId = null,
                FleetName = null,
                GameVersionId = null,
            };
            UserContext = new UserContext
            {
                UserId = null,
                Nickname = null
            };
            ConnectionContext = new ConnectionContext
            {
                Region = null
            };
            RoomContext = new RoomContext()
            {
                MatchId = null,
            };
            PlayPadContext = new PlayPadContext
            {
                ProtocolVersion = null,
                SdkVersion = null,
                Capabilities = null,
                TournamentId = null,
                FeatureAccess = null
            };
            Context = null;
            MethodName = null;
            if (ElympicsLogger.CurrentContext.HasValue)
                return;

            ElympicsLogger.CurrentContext = this;
        }

        public void Log(string message) => ElympicsLogger.Log(message, TimeUtil.DateTimeNowToString, this);

        public void Warning(string message) => ElympicsLogger.LogWarning(message, TimeUtil.DateTimeNowToString, this);

        public void Error(string message) => ElympicsLogger.LogError(message, TimeUtil.DateTimeNowToString, this);

        public Exception CaptureAndThrow(Exception exception) => ElympicsLogger.CaptureAndThrow(exception, TimeUtil.DateTimeNowToString, this);
        public void Exception(Exception exception) => ElympicsLogger.LogException(exception, TimeUtil.DateTimeNowToString, this);

        public override string ToString() =>
            $"{nameof(Context)}: {Context} | "
            + $"{nameof(MethodName)}: {MethodName} | "
            + $"{Environment.NewLine}{ElympicsContext}"
            + $"{Environment.NewLine}{UserContext}"
            + $"{Environment.NewLine}{ConnectionContext}"
            + $"{Environment.NewLine}{RoomContext}"
            + $"{Environment.NewLine}{PlayPadContext}";
    }

    internal class ElympicsContext
    {
        public string SdkVersion;
        public string GameId;
        public string FleetName;
        public string GameVersionId;

        public void Clear()
        {
            SdkVersion = string.Empty;
            GameId = string.Empty;
            FleetName = string.Empty;
            GameVersionId = string.Empty;
        }

        public override string ToString() => $"{nameof(SdkVersion)}: {SdkVersion} | " + $"{nameof(GameId)}: {GameId} | " + $"{nameof(FleetName)}: {FleetName} | " + $"{nameof(GameVersionId)}: {GameVersionId} | ";
    }

    internal class UserContext
    {
        public string UserId;
        public string Nickname;
        public string AuthType;
        public string WalletAddress;

        public void Clear()
        {
            UserId = string.Empty;
            Nickname = string.Empty;
            AuthType = string.Empty;
            WalletAddress = string.Empty;
        }

        public override string ToString() =>
            $"{nameof(UserId)}: {UserId} | " + $"{nameof(Nickname)}: {Nickname} | " + $"{nameof(AuthType)}: {AuthType} | " + $"{nameof(WalletAddress)}: {WalletAddress} | ";
    }

    internal class ConnectionContext
    {
        public string Region;
        public string LobbyUrl;

        public void Clear() => Region = string.Empty;

        public override string ToString() => $"{nameof(Region)}: {Region} | " + $"{nameof(LobbyUrl)}: {LobbyUrl} | ";
    }

    internal class RoomContext
    {
        public string RoomId;
        public string QueueName;
        public string MatchId;
        public string TcpUdpServerAddress;
        public string WebServerAddress;

        public void Clear()
        {
            RoomId = string.Empty;
            QueueName = string.Empty;
            MatchId = string.Empty;
            TcpUdpServerAddress = string.Empty;
            WebServerAddress = string.Empty;
        }
        public override string ToString() => $"{nameof(RoomId)}: {RoomId} | "
            + $"{nameof(QueueName)}: {QueueName} | "
            + $"{nameof(MatchId)}: {MatchId} | "
            + $"{nameof(TcpUdpServerAddress)}: {TcpUdpServerAddress} | "
            + $"{nameof(WebServerAddress)}: {WebServerAddress} | ";
    }

    internal class PlayPadContext
    {
        public string SdkVersion;
        public string ProtocolVersion;
        public string Capabilities;
        public string TournamentId;
        public string FeatureAccess;

        public override string ToString() => $"{nameof(Capabilities)}: {Capabilities} | "
            + $"{nameof(TournamentId)}: {TournamentId} | "
            + $"{nameof(FeatureAccess)}: {FeatureAccess} | "
            + $"{nameof(SdkVersion)}: {SdkVersion} | "
            + $"{nameof(ProtocolVersion)}: {ProtocolVersion} | ";
    }
}
