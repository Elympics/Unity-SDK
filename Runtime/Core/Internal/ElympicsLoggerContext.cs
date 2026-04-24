#nullable enable
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
        public WebRtcContext WebRtcContext;
        public RoomContext RoomContext;
        public PlayPadContext PlayPadContext;
        public string Context;
        public string MethodName;
        public string GameMode;

        public ElympicsLoggerContext(Guid sessionId)
        {
            App = string.Empty;
            SessionId = sessionId;
            ElympicsContext = new ElympicsContext();
            UserContext = new UserContext();
            ConnectionContext = new ConnectionContext();
            WebRtcContext = new WebRtcContext();
            RoomContext = new RoomContext();
            PlayPadContext = new PlayPadContext();
            Context = string.Empty;
            MethodName = string.Empty;
            GameMode = string.Empty;
        }

        public ElympicsLoggerContext Copy() => new()
        {
            SessionId = SessionId,
            App = App,
            ElympicsContext = ElympicsContext,
            UserContext = UserContext,
            ConnectionContext = ConnectionContext,
            WebRtcContext = WebRtcContext,
            RoomContext = RoomContext,
            PlayPadContext = PlayPadContext,
            Context = Context,
            MethodName = MethodName,
            GameMode = GameMode,
        };

        public override string ToString() =>
            $"{nameof(Context)}: {Context} | "
            + $"{nameof(MethodName)}: {MethodName} | "
            + $"{Environment.NewLine}{ElympicsContext}"
            + $"{Environment.NewLine}{UserContext}"
            + $"{Environment.NewLine}{ConnectionContext}"
            + $"{Environment.NewLine}{WebRtcContext}"
            + $"{Environment.NewLine}{RoomContext}"
            + $"{Environment.NewLine}{PlayPadContext}";
    }

    internal class ElympicsContext
    {
        public string SdkVersion = string.Empty;
        public string GameId = string.Empty;
        public string FleetName = string.Empty;
        public string GameVersionId = string.Empty;

        public void Clear()
        {
            SdkVersion = string.Empty;
            GameId = string.Empty;
            FleetName = string.Empty;
            GameVersionId = string.Empty;
        }

        public override string ToString() => $"{nameof(SdkVersion)}: {SdkVersion} | "
            + $"{nameof(GameId)}: {GameId} | "
            + $"{nameof(FleetName)}: {FleetName} | "
            + $"{nameof(GameVersionId)}: {GameVersionId} | ";
    }

    internal class UserContext
    {
        public string UserId = string.Empty;
        public string Nickname = string.Empty;
        public string AuthType = string.Empty;
        public string WalletAddress = string.Empty;

        public void Clear()
        {
            UserId = string.Empty;
            Nickname = string.Empty;
            AuthType = string.Empty;
            WalletAddress = string.Empty;
        }

        public override string ToString() =>
            $"{nameof(UserId)}: {UserId} | "
            + $"{nameof(Nickname)}: {Nickname} | "
            + $"{nameof(AuthType)}: {AuthType} | "
            + $"{nameof(WalletAddress)}: {WalletAddress} | ";
    }

    internal class ConnectionContext
    {
        public string Region = string.Empty;
        public string LobbyUrl = string.Empty;

        public void Clear() => Region = string.Empty;

        public override string ToString() =>
            $"{nameof(Region)}: {Region} | "
            + $"{nameof(LobbyUrl)}: {LobbyUrl} | ";
    }

    internal class WebRtcContext
    {
        public bool UsesTurn;

        public void Clear() => UsesTurn = false;

        public override string ToString() =>
            $"{nameof(UsesTurn)}: {UsesTurn} | ";
    }

    internal class RoomContext
    {
        public string RoomId = string.Empty;
        public string QueueName = string.Empty;
        public string MatchId = string.Empty;
        public string TcpUdpServerAddress = string.Empty;
        public string WebServerAddress = string.Empty;

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
        public string SdkVersion = string.Empty;
        public string ProtocolVersion = string.Empty;
        public string Capabilities = string.Empty;
        public string TournamentId = string.Empty;
        public string FeatureAccess = string.Empty;

        public override string ToString() => $"{nameof(Capabilities)}: {Capabilities} | "
            + $"{nameof(TournamentId)}: {TournamentId} | "
            + $"{nameof(FeatureAccess)}: {FeatureAccess} | "
            + $"{nameof(SdkVersion)}: {SdkVersion} | "
            + $"{nameof(ProtocolVersion)}: {ProtocolVersion} | ";
    }
}
