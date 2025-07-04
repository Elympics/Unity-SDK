using System.Runtime.CompilerServices;
using Elympics.Models.Authentication;

namespace Elympics.ElympicsSystems.Internal
{
    internal static class LoggerContextBuilder
    {
        #region Modify Context

        public static ElympicsLoggerContext SetRegion(this ElympicsLoggerContext current, string region)
        {
            current.ConnectionContext.Region = region;
            return current;
        }

        public static ElympicsLoggerContext SetLobbyUrl(this ElympicsLoggerContext current, string url)
        {
            current.ConnectionContext.LobbyUrl = url;
            return current;
        }

        public static ElympicsLoggerContext SetUserId(this ElympicsLoggerContext current, string userId)
        {
            current.UserContext.UserId = userId;
            return current;
        }

        public static ElympicsLoggerContext SetNickname(this ElympicsLoggerContext current, string nickname)
        {
            current.UserContext.Nickname = nickname;
            return current;
        }

        public static ElympicsLoggerContext SetNoUser(this ElympicsLoggerContext current)
        {
            current.UserContext.Clear();
            return current;
        }

        public static ElympicsLoggerContext SetNoConnection(this ElympicsLoggerContext current)
        {
            current.ConnectionContext.Clear();
            return current;
        }

        public static ElympicsLoggerContext SetAuthType(this ElympicsLoggerContext current, AuthType authType)
        {
            current.UserContext.AuthType = authType.ToString();
            return current;
        }

        public static ElympicsLoggerContext SetWalletAddress(this ElympicsLoggerContext current, string walletAddress)
        {
            current.UserContext.WalletAddress = walletAddress;
            return current;
        }

        public static ElympicsLoggerContext SetCapabilities(this ElympicsLoggerContext current, string capabilities)
        {
            current.PlayPadContext.Capabilities = capabilities;
            return current;
        }

        public static ElympicsLoggerContext SetFeatureAccess(this ElympicsLoggerContext current, string featureAccess)
        {
            current.PlayPadContext.FeatureAccess = featureAccess;
            return current;
        }

        public static ElympicsLoggerContext SetTournamentId(this ElympicsLoggerContext current, string tournamentId)
        {
            current.PlayPadContext.TournamentId = tournamentId;
            return current;
        }


        public static ElympicsLoggerContext SetQueue(this ElympicsLoggerContext current, string queueName)
        {
            current.RoomContext.QueueName = queueName;
            return current;
        }

        public static ElympicsLoggerContext SetRoomId(this ElympicsLoggerContext current, string roomId)
        {
            current.RoomContext.RoomId = roomId;
            return current;
        }

        public static ElympicsLoggerContext SetMatchId(this ElympicsLoggerContext current, string matchId)
        {
            current.RoomContext.MatchId = matchId;
            return current;
        }

        public static ElympicsLoggerContext SetServerAddress(this ElympicsLoggerContext current, string tcpUdpServerAddress = "", string webServerAddress = "")
        {
            current.RoomContext.TcpUdpServerAddress = tcpUdpServerAddress;
            current.RoomContext.WebServerAddress = webServerAddress;
            return current;
        }

        public static ElympicsLoggerContext SetElympicsContext(this ElympicsLoggerContext current, string elympicsSdk, string gameId)
        {
            current.ElympicsContext.SdkVersion = elympicsSdk;
            current.ElympicsContext.GameId = gameId;
            return current;
        }

        public static ElympicsLoggerContext SetPlayPadSdkContext(this ElympicsLoggerContext current, string protocolVersion, string sdkVersion)
        {
            current.PlayPadContext.SdkVersion = sdkVersion;
            current.PlayPadContext.ProtocolVersion = protocolVersion;
            return current;
        }

        public static ElympicsLoggerContext SetFleetName(this ElympicsLoggerContext current, string fleetName)
        {
            current.ElympicsContext.FleetName = fleetName;
            return current;
        }

        public static ElympicsLoggerContext SetGameVersionId(this ElympicsLoggerContext current, string gameVersionId)
        {
            current.ElympicsContext.GameVersionId = gameVersionId;
            return current;
        }

        public static ElympicsLoggerContext SetNoRoom(this ElympicsLoggerContext current)
        {
            current.RoomContext.Clear();
            return current;
        }
        public static ElympicsLoggerContext SetGameMode(this ElympicsLoggerContext current, string gameMode)
        {
            current.GameMode = gameMode;
            return current;
        }

        #endregion

        #region Return New Context

        public static ElympicsLoggerContext WithMethodName(this ElympicsLoggerContext current, [CallerMemberName] string methodName = "")
        {
            var logger = new ElympicsLoggerContext
            {
                SessionId = current.SessionId,
                App = current.App,
                Context = current.Context,
                MethodName = methodName,
                ElympicsContext = current.ElympicsContext,
                UserContext = current.UserContext,
                ConnectionContext = current.ConnectionContext,
                PlayPadContext = current.PlayPadContext,
                RoomContext = current.RoomContext,
                GameMode = current.GameMode,
            };
            return logger;
        }

        public static ElympicsLoggerContext WithApp(this ElympicsLoggerContext current, string app)
        {
            var logger = new ElympicsLoggerContext
            {
                SessionId = current.SessionId,
                App = app,
                Context = current.Context,
                MethodName = current.MethodName,
                ElympicsContext = current.ElympicsContext,
                UserContext = current.UserContext,
                ConnectionContext = current.ConnectionContext,
                PlayPadContext = current.PlayPadContext,
                RoomContext = current.RoomContext,
                GameMode = current.GameMode,
            };
            return logger;
        }

        public static ElympicsLoggerContext WithContext(this ElympicsLoggerContext current, string context)
        {
            var logger = new ElympicsLoggerContext
            {
                SessionId = current.SessionId,
                App = current.App,
                Context = context,
                MethodName = current.MethodName,
                ElympicsContext = current.ElympicsContext,
                UserContext = current.UserContext,
                ConnectionContext = current.ConnectionContext,
                PlayPadContext = current.PlayPadContext,
                RoomContext = current.RoomContext,
                GameMode = current.GameMode,
            };
            return logger;
        }

        #endregion
    }
}
