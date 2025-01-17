using System.Runtime.CompilerServices;
using Elympics.Models.Authentication;

namespace Elympics.ElympicsSystems.Internal
{
    internal static class LoggerContextBuilder
    {
        public static ElympicsLoggerContext WithRegion(this ElympicsLoggerContext current, string region)
        {
            current.ConnectionContext.Region = region;
            return current;
        }

        public static ElympicsLoggerContext WithLobbyUrl(this ElympicsLoggerContext current, string url)
        {
            current.ConnectionContext.LobbyUrl = url;
            return current;
        }

        public static ElympicsLoggerContext WithUserId(this ElympicsLoggerContext current, string userId)
        {
            current.UserContext.UserId = userId;
            return current;
        }

        public static ElympicsLoggerContext WithNickname(this ElympicsLoggerContext current, string nickname)
        {
            current.UserContext.Nickname = nickname;
            return current;
        }

        public static ElympicsLoggerContext WithNoUser(this ElympicsLoggerContext current)
        {
            current.UserContext.Clear();
            return current;
        }

        public static ElympicsLoggerContext WithNoConnection(this ElympicsLoggerContext current)
        {
            current.ConnectionContext.Clear();
            return current;
        }

        public static ElympicsLoggerContext WithAuthType(this ElympicsLoggerContext current, AuthType authType)
        {
            current.UserContext.AuthType = authType.ToString();
            return current;
        }

        public static ElympicsLoggerContext WithWalletAddress(this ElympicsLoggerContext current, string walletAddress)
        {
            current.UserContext.WalletAddress = walletAddress;
            return current;
        }

        public static ElympicsLoggerContext WithCapabilities(this ElympicsLoggerContext current, string capabilities)
        {
            current.PlayPadContext.Capabilities = capabilities;
            return current;
        }

        public static ElympicsLoggerContext WithFeatureAccess(this ElympicsLoggerContext current, string featureAccess)
        {
            current.PlayPadContext.FeatureAccess = featureAccess;
            return current;
        }

        public static ElympicsLoggerContext WithTournamentId(this ElympicsLoggerContext current, string tournamentId)
        {
            current.PlayPadContext.TournamentId = tournamentId;
            return current;
        }

        public static ElympicsLoggerContext WithMethodName(this ElympicsLoggerContext current, [CallerMemberName] string methodName = "")
        {
            var logger = new ElympicsLoggerContext
            {
                App = current.App,
                Context = current.Context,
                MethodName = methodName,
                AppContext = current.AppContext,
                UserContext = current.UserContext,
                ConnectionContext = current.ConnectionContext,
                PlayPadContext = current.PlayPadContext,
                RoomContext = current.RoomContext,
                LogMessage = current.LogMessage,
                Time = current.Time,
            };
            return logger;
        }

        public static ElympicsLoggerContext WithQueue(this ElympicsLoggerContext current, string queueName)
        {
            current.RoomContext.QueueName = queueName;
            return current;
        }

        public static ElympicsLoggerContext WithRoomId(this ElympicsLoggerContext current, string roomId)
        {
            current.RoomContext.RoomId = roomId;
            return current;
        }

        public static ElympicsLoggerContext WithMatchId(this ElympicsLoggerContext current, string matchId)
        {
            current.RoomContext.MatchId = matchId;
            return current;
        }

        public static ElympicsLoggerContext WithServerAddress(this ElympicsLoggerContext current, string tcpUdpServerAddress = "", string webServerAddress = "")
        {
            current.RoomContext.TcpUdpServerAddress = tcpUdpServerAddress;
            current.RoomContext.WebServerAddress = webServerAddress;
            return current;
        }

        public static ElympicsLoggerContext WithNoRoom(this ElympicsLoggerContext current)
        {
            current.RoomContext.Clear();
            return current;
        }

        public static ElympicsLoggerContext WithApp(this ElympicsLoggerContext current, string app)
        {
            var logger = new ElympicsLoggerContext
            {
                App = app,
                Context = current.Context,
                MethodName = current.MethodName,
                AppContext = current.AppContext,
                UserContext = current.UserContext,
                ConnectionContext = current.ConnectionContext,
                PlayPadContext = current.PlayPadContext,
                RoomContext = current.RoomContext,
                LogMessage = current.LogMessage,
                Time = current.Time,
            };
            return logger;
        }

        public static ElympicsLoggerContext WithContext(this ElympicsLoggerContext current, string context)
        {
            var logger = new ElympicsLoggerContext
            {
                App = current.App,
                Context = context,
                MethodName = current.MethodName,
                AppContext = current.AppContext,
                UserContext = current.UserContext,
                ConnectionContext = current.ConnectionContext,
                PlayPadContext = current.PlayPadContext,
                RoomContext = current.RoomContext,
                LogMessage = current.LogMessage,
                Time = current.Time,
            };
            return logger;
        }
    }
}
