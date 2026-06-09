#nullable enable
using System;
using Elympics.Core.Logger.State;
using Elympics.Models.Authentication;

namespace Elympics.Core.Logger
{
    [Serializable]
    internal class ApplicationState : IVisitableState
    {
        private readonly SdkState _sdk;
        private GameState? _game;
        private UserState? _user;
        private LobbyState? _lobby;
        private PlayPadState? _playPad;
        private RoomState? _room;
        private GameServerState? _gameServer;

        public ApplicationState(string sdkVersion) => _sdk = new SdkState(sdkVersion);

        public ApplicationState SetCloudUrls(string apiUrl, string gameServerUrl)
        {
            _sdk.ApiUrl = apiUrl;
            _sdk.GameServerUrl = gameServerUrl;
            return this;
        }

        public ApplicationState SetGame(Guid gameId, string versionName)
        {
            var gameIdStringified = gameId.ToString();
            if ((_game?.GameId, _game?.VersionName) != (gameIdStringified, versionName))
                _game = new GameState(gameId.ToString(), versionName);
            return this;
        }

        public ApplicationState SetGameName(string gameName)
        {
            if (_game is null)
                throw new InvalidOperationException("Game state is not set");
            _game.GameName = gameName;
            return this;
        }

        public ApplicationState SetGameMode(string gameMode)
        {
            if (_game is null)
                throw new InvalidOperationException("Game state is not set");
            _game.GameMode = gameMode;
            return this;
        }

        public ApplicationState SetGameVersionId(string gameVersionId)
        {
            if (_game is null)
                throw new InvalidOperationException("Game state is not set");
            _game.GameVersionId = gameVersionId;
            return this;
        }

        public ApplicationState SetFleetName(string fleetName)
        {
            if (_game is null)
                throw new InvalidOperationException("Game state is not set");
            _game.FleetName = fleetName;
            return this;
        }

        public ApplicationState SetUserId(Guid userId)
        {
            var userIdStringified = userId.ToString();
            if (_user?.UserId != userIdStringified)
                _user = new UserState(userId.ToString());
            return this;
        }

        public ApplicationState SetNickname(string nickname)
        {
            if (_user is null)
                throw new InvalidOperationException("User state is not set");
            _user.Nickname = nickname;
            return this;
        }

        public ApplicationState SetAuthType(AuthType authType)
        {
            if (_user is null)
                throw new InvalidOperationException("User state is not set");
            _user.AuthType = authType.ToString();
            return this;
        }

        public ApplicationState SetWalletAddress(string walletAddress)
        {
            if (_user is null)
                throw new InvalidOperationException("User state is not set");
            _user.WalletAddress = walletAddress;
            return this;
        }

        public ApplicationState SetNoUser()
        {
            _user = null;
            return this;
        }

        public ApplicationState SetRegion(string region)
        {
            if (_lobby?.Region != region)
                _lobby = new LobbyState(region);
            return this;
        }

        public ApplicationState SetNoConnection()
        {
            _lobby = null;
            return this;
        }

        public ApplicationState SetPlayPadVersion(string protocolVersion)
        {
            if (_playPad?.ProtocolVersion != protocolVersion)
                _playPad = new PlayPadState(protocolVersion);
            return this;
        }

        public ApplicationState SetCapabilities(string capabilities)
        {
            if (_playPad is null)
                throw new InvalidOperationException("PlayPad state is not set");
            _playPad.Capabilities = capabilities;
            return this;
        }

        public ApplicationState SetFeatureAccess(string featureAccess)
        {
            if (_playPad is null)
                throw new InvalidOperationException("PlayPad state is not set");
            _playPad.FeatureAccess = featureAccess;
            return this;
        }

        public ApplicationState SetTournamentId(string tournamentId)
        {
            if (_playPad is null)
                throw new InvalidOperationException("PlayPad state is not set");
            _playPad.TournamentId = tournamentId;
            return this;
        }

        public ApplicationState SetRoomId(string roomId)
        {
            if (_room?.RoomId != roomId)
                _room = new RoomState(roomId);
            return this;
        }

        public ApplicationState SetQueue(string queueName)
        {
            if (_room is null)
                throw new InvalidOperationException("Room state is not set");
            _room.QueueName = queueName;
            return this;
        }

        public ApplicationState SetMatchId(string matchId)
        {
            if (_room is null)
                throw new InvalidOperationException("Room state is not set");
            _room.MatchId = matchId;
            return this;
        }

        public ApplicationState SetNoRoom()
        {
            _room = null;
            return this;
        }

        public ApplicationState SetTcpUdpServerAddress(string serverAddress)
        {
            if (_gameServer is not TcpUdpState state || state.ServerAddress != serverAddress)
                _gameServer = new TcpUdpState(serverAddress);
            return this;
        }

        public ApplicationState SetWebRtcServerAddress(string serverAddress)
        {
            if (_gameServer is not WebRtcState state || state.ServerAddress != serverAddress)
                _gameServer = new WebRtcState(serverAddress);
            return this;
        }

        public ApplicationState SetUsesTurn(bool usesTurn = true)
        {
            if (_gameServer is not WebRtcState webRtcState)
                throw new InvalidOperationException("WebRtc state is not set");
            webRtcState.UsesTurn = usesTurn;
            return this;
        }

        public void Visit(IStateVisitor visitor)
        {
            visitor.ProcessSubstate(nameof(SdkState));
            _sdk.Visit(visitor);

            if (_game is not null)
            {
                visitor.ProcessSubstate(nameof(GameState));
                _game.Visit(visitor);
            }

            if (_user is not null)
            {
                visitor.ProcessSubstate(nameof(UserState));
                _user.Visit(visitor);
            }

            if (_lobby is not null)
            {
                visitor.ProcessSubstate(nameof(LobbyState));
                _lobby.Visit(visitor);
            }

            if (_playPad is not null)
            {
                visitor.ProcessSubstate(nameof(PlayPadState));
                _playPad.Visit(visitor);
            }

            if (_room is not null)
            {
                visitor.ProcessSubstate(nameof(RoomState));
                _room.Visit(visitor);
            }

            if (_gameServer is not null)
            {
                visitor.ProcessSubstate(nameof(GameServerState));
                _gameServer.Visit(visitor);
            }
        }
    }
}
