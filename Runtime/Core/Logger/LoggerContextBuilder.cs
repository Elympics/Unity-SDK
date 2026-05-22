using System.Runtime.CompilerServices;
using Elympics.Models.Authentication;

namespace Elympics.Core.Logger
{
    internal static class LoggerContextBuilder
    {
        #region Modify Context

        public static ApplicationState SetRegion(this ApplicationState current, string region)
        {
            current.ConnectionContext.Region = region;
            return current;
        }

        public static ApplicationState SetLobbyUrl(this ApplicationState current, string url)
        {
            current.ConnectionContext.LobbyUrl = url;
            return current;
        }

        public static ApplicationState SetUserId(this ApplicationState current, string userId)
        {
            current.UserContext.UserId = userId;
            return current;
        }

        public static ApplicationState SetNickname(this ApplicationState current, string nickname)
        {
            current.UserContext.Nickname = nickname;
            return current;
        }

        public static ApplicationState SetNoUser(this ApplicationState current)
        {
            current.UserContext.Clear();
            return current;
        }

        public static ApplicationState SetNoConnection(this ApplicationState current)
        {
            current.ConnectionContext.Clear();
            return current;
        }

        public static ApplicationState SetAuthType(this ApplicationState current, AuthType authType)
        {
            current.UserContext.AuthType = authType.ToString();
            return current;
        }

        public static ApplicationState SetWalletAddress(this ApplicationState current, string walletAddress)
        {
            current.UserContext.WalletAddress = walletAddress;
            return current;
        }

        public static ApplicationState SetCapabilities(this ApplicationState current, string capabilities)
        {
            current.PlayPadContext.Capabilities = capabilities;
            return current;
        }

        public static ApplicationState SetFeatureAccess(this ApplicationState current, string featureAccess)
        {
            current.PlayPadContext.FeatureAccess = featureAccess;
            return current;
        }

        public static ApplicationState SetTournamentId(this ApplicationState current, string tournamentId)
        {
            current.PlayPadContext.TournamentId = tournamentId;
            return current;
        }


        public static ApplicationState SetQueue(this ApplicationState current, string queueName)
        {
            current.RoomContext.QueueName = queueName;
            return current;
        }

        public static ApplicationState SetRoomId(this ApplicationState current, string roomId)
        {
            current.RoomContext.RoomId = roomId;
            return current;
        }

        public static ApplicationState SetMatchId(this ApplicationState current, string matchId)
        {
            current.RoomContext.MatchId = matchId;
            return current;
        }

        public static ApplicationState SetServerAddress(this ApplicationState current, string tcpUdpServerAddress = "", string webServerAddress = "")
        {
            current.RoomContext.TcpUdpServerAddress = tcpUdpServerAddress;
            current.RoomContext.WebServerAddress = webServerAddress;
            return current;
        }

        public static ApplicationState SetElympicsContext(this ApplicationState current, string elympicsSdk, string gameId)
        {
            current.ElympicsContext.SdkVersion = elympicsSdk;
            current.ElympicsContext.GameId = gameId;
            return current;
        }

        public static ApplicationState SetPlayPadSdkContext(this ApplicationState current, string protocolVersion, string sdkVersion)
        {
            current.PlayPadContext.SdkVersion = sdkVersion;
            current.PlayPadContext.ProtocolVersion = protocolVersion;
            return current;
        }

        public static ApplicationState SetFleetName(this ApplicationState current, string fleetName)
        {
            current.ElympicsContext.FleetName = fleetName;
            return current;
        }

        public static ApplicationState SetGameVersionId(this ApplicationState current, string gameVersionId)
        {
            current.ElympicsContext.GameVersionId = gameVersionId;
            return current;
        }

        public static ApplicationState SetNoRoom(this ApplicationState current)
        {
            current.RoomContext.Clear();
            return current;
        }

        public static ApplicationState SetGameMode(this ApplicationState current, string gameMode)
        {
            current.GameMode = gameMode;
            return current;
        }

        public static ApplicationState SetUsesTurn(this ApplicationState current, bool usesTurn = true)
        {
            current.WebRtcContext.UsesTurn = usesTurn;
            return current;
        }

        #endregion

        #region Return New Context

        public static ApplicationState WithMethodName(this ApplicationState current, [CallerMemberName] string methodName = "")
        {
            var logger = current.Copy();
            logger.MethodName = methodName;
            return logger;
        }

        public static ApplicationState WithApp(this ApplicationState current, string app)
        {
            var logger = current.Copy();
            logger.App = app;
            return logger;
        }

        public static ApplicationState WithContext(this ApplicationState current, string context)
        {
            var logger = current.Copy();
            logger.Context = context;
            return logger;
        }

        #endregion
    }
}
