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
        private const string LogFormat = "[{0,-28}] [{1}]";
        public Guid SessionId;
        public string App;
        public AppContext AppContext;
        public UserContext UserContext;
        public ConnectionContext ConnectionContext;
        public RoomContext RoomContext;
        public PlayPadContext PlayPadContext;
        public string Context;
        public string MethodName;
        public string Time;
        public string LogMessage;

        public ElympicsLoggerContext(Guid sessionId, string version, string gameId)
        {
            App = null;
            SessionId = sessionId;
            AppContext = new AppContext
            {
                Version = version,
                GameId = gameId
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
                Capabilities = null,
                TournamentId = null,
                FeatureAccess = null
            };
            Context = null;
            LogMessage = null;
            Time = null;
            MethodName = null;
            if (ElympicsLogger.CurrentContext.HasValue)
                return;

            ElympicsLogger.CurrentContext = this;
        }

        public void Log(string message)
        {
            LogMessage = message;
            Time = DateTime.UtcNow.ToString(ElympicsLogger.TimeFormat);
            ElympicsLogger.Log(this);
        }

        public void Warning(string message)
        {
            LogMessage = message;
            Time = DateTime.UtcNow.ToString(ElympicsLogger.TimeFormat);
            ElympicsLogger.LogWarning(this);
        }

        public void Error(string message)
        {
            LogMessage = message;
            Time = DateTime.UtcNow.ToString(ElympicsLogger.TimeFormat);
            ElympicsLogger.LogError(this);
        }

        public Exception CaptureAndThrow(Exception exception)
        {
            LogMessage = exception.Message;
            Time = DateTime.UtcNow.ToString(ElympicsLogger.TimeFormat);
            return ElympicsLogger.CaptureAndThrow(exception, this);
        }
        public void Exception(Exception exception)
        {
            LogMessage = exception.Message;
            Time = DateTime.UtcNow.ToString(ElympicsLogger.TimeFormat);
            ElympicsLogger.LogException(exception, this);
        }

        public override string ToString() =>
            $"{string.Format(LogFormat, Time, App)} {LogMessage}{Environment.NewLine}" + $"{nameof(Context)}: {Context} | " + $"{nameof(MethodName)}: {MethodName} | " + $"{Environment.NewLine}{AppContext}" + $"{Environment.NewLine}{UserContext}" + $"{Environment.NewLine}{ConnectionContext}" + $"{Environment.NewLine}{RoomContext}" + $"{Environment.NewLine}{PlayPadContext}";
    }

    internal class AppContext
    {
        public string Version;
        public string GameId;

        public void Clear()
        {
            Version = string.Empty;
            GameId = string.Empty;
        }

        public override string ToString() => $"{nameof(Version)}: {Version} | " + $"{nameof(GameId)}: {GameId} | ";
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

        public override string ToString() => $"{nameof(UserId)}: {UserId} | " + $"{nameof(Nickname)}: {Nickname} | " + $"{nameof(AuthType)}: {AuthType} | " + $"{nameof(WalletAddress)}: {WalletAddress} | ";
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
        public override string ToString() => $"{nameof(RoomId)}: {RoomId} | " + $"{nameof(QueueName)}: {QueueName} | " + $"{nameof(MatchId)}: {MatchId} | " + $"{nameof(TcpUdpServerAddress)}: {TcpUdpServerAddress} | " + $"{nameof(WebServerAddress)}: {WebServerAddress} | ";
    }

    internal class PlayPadContext
    {
        public string Capabilities;
        public string TournamentId;
        public string FeatureAccess;

        public override string ToString() => $"{nameof(Capabilities)}: {Capabilities} | " + $"{nameof(TournamentId)}: {TournamentId} | " + $"{nameof(FeatureAccess)}: {FeatureAccess} | ";
    }
}
