#nullable enable
using System;
using Elympics.Core.Logger.State;
using Elympics.Models.Authentication;

namespace Elympics.Core.Logger
{
    [Serializable]
    internal class ApplicationState : IVisitableState
    {
        internal string SessionId => _sdk.SessionId;

        private readonly SdkState _sdk;
        private GameState _game;
        private UserState _user;
        private MatchState _match;
        private GameServerState _gameServer;
        private PlayPadState _playPad;

        public ApplicationState(string sdkVersion)
        {
            _sdk = new SdkState(sdkVersion);
            _game = new GameState();
            _user = new UserState();
            _match = new MatchState();
            _gameServer = new GameServerState();
            _playPad = new PlayPadState();
        }

        public void SetCloudUrls(string? apiUrl, string? gameServerUrl)
        {
            _sdk.ApiUrl = apiUrl;
            _sdk.GameServerUrl = gameServerUrl;
        }

        public void SetGame(Guid gameId, string versionName)
        {
            var gameIdStringified = gameId.ToString();
            var oldGameData = (_game.GameId, _game.VersionName);
            var newGameData = (gameIdStringified, versionName);
            if (oldGameData != (null, null) && oldGameData != newGameData)
                _game = new GameState();
            _game.GameId = gameIdStringified;
            _game.VersionName = versionName;
        }

        public void SetGameName(string? gameName) => _game.GameName = gameName;
        public void SetGameMode(string? gameMode) => _game.GameMode = gameMode;
        public void SetGameVersionId(string? gameVersionId) => _game.GameVersionId = gameVersionId;
        public void SetFleetName(string? fleetName) => _game.FleetName = fleetName;
        public void ClearGame() => _game = new GameState();

        public void SetUserId(Guid userId)
        {
            var userIdStringified = userId.ToString();
            if (_user.UserId != null && _user.UserId != userIdStringified)
                _user = new UserState();
            _user.UserId = userIdStringified;
        }

        public void SetNickname(string? nickname) => _user.Nickname = nickname;
        public void SetAuthType(AuthType? authType) => _user.AuthType = authType?.ToString();
        public void SetWalletAddress(string? walletAddress) => _user.WalletAddress = walletAddress;
        public void ClearUser() => _user = new UserState();

        public void SetRegion(string? region) => _sdk.Region = region;

        public void SetPlayPadVersion(string? protocolVersion)
        {
            if (_playPad.ProtocolVersion != null && _playPad.ProtocolVersion != protocolVersion)
                _playPad = new PlayPadState();
            _playPad.ProtocolVersion = protocolVersion;
        }

        public void SetPlayPadCapabilities(string? capabilities) => _playPad.Capabilities = capabilities;
        public void SetPlayPadFeatureAccess(string? featureAccess) => _playPad.FeatureAccess = featureAccess;
        public void SetTournamentId(string? tournamentId) => _playPad.TournamentId = tournamentId;
        public void ClearPlayPad() => _playPad = new PlayPadState();

        public void SetRoomId(string? roomId) => _match.RoomId = roomId;
        public void SetQueue(string? queueName) => _match.QueueName = queueName;
        public void SetMatchId(string? matchId) => _match.MatchId = matchId;
        public void ClearRoom() => _match = new MatchState();

        public void SetTcpUdp() => _gameServer.ConnectionType = "TCP+UDP";
        public void SetWebRtc() => _gameServer.ConnectionType = "WebRTC";
        public void SetGameServerAddress(string? tcpUdpAddress, string? webRtcAddress)
        {
            _gameServer.TcpUdpServerAddress = tcpUdpAddress;
            _gameServer.WebServerAddress = webRtcAddress;
        }

        public void SetUsesTurn(bool? usesTurn = true) => _gameServer.UsesTurn = usesTurn;
        public void ClearGameServer() => _gameServer = new GameServerState();

        public bool Visit(IStateVisitor visitor)
        {
            visitor.ProcessSubstate(nameof(SdkState));
            _ = _sdk.Visit(visitor);

            if (_game.Visit(PassthroughVisitor.Instance))
            {
                visitor.ProcessSubstate(nameof(GameState));
                _ = _game.Visit(visitor);
            }

            if (_user.Visit(PassthroughVisitor.Instance))
            {
                visitor.ProcessSubstate(nameof(UserState));
                _ = _user.Visit(visitor);
            }

            if (_match.Visit(PassthroughVisitor.Instance))
            {
                visitor.ProcessSubstate(nameof(MatchState));
                _ = _match.Visit(visitor);
            }

            if (_gameServer.Visit(PassthroughVisitor.Instance))
            {
                visitor.ProcessSubstate(nameof(GameServerState));
                _ = _gameServer.Visit(visitor);
            }

            if (_playPad.Visit(PassthroughVisitor.Instance))
            {
                visitor.ProcessSubstate(nameof(PlayPadState));
                _ = _playPad.Visit(visitor);
            }

            return true;
        }
    }
}
