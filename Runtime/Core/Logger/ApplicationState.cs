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

        public void SetCloudUrls(string? apiUrl, string? gameServerUrl)
        {
            _sdk.ApiUrl = apiUrl;
            _sdk.GameServerUrl = gameServerUrl;
        }

        public void SetGame(Guid gameId, string versionName)
        {
            var gameIdStringified = gameId.ToString();
            if ((_game?.GameId, _game?.VersionName) != (gameIdStringified, versionName))
                _game = new GameState(gameId.ToString(), versionName);
        }

        public void SetGameName(string? gameName)
        {
            if (_game is null)
                throw new InvalidOperationException("Game state is not set");
            _game.GameName = gameName;
        }

        public void SetGameMode(string? gameMode)
        {
            if (_game is null)
                throw new InvalidOperationException("Game state is not set");
            _game.GameMode = gameMode;
        }

        public void SetGameVersionId(string? gameVersionId)
        {
            if (_game is null)
                throw new InvalidOperationException("Game state is not set");
            _game.GameVersionId = gameVersionId;
        }

        public void SetFleetName(string? fleetName)
        {
            if (_game is null)
                throw new InvalidOperationException("Game state is not set");
            _game.FleetName = fleetName;
        }

        public void ClearGame() => _game = null;

        public void SetUserId(Guid userId)
        {
            var userIdStringified = userId.ToString();
            if (_user?.UserId != userIdStringified)
                _user = new UserState(userId.ToString());
        }

        public void SetNickname(string? nickname)
        {
            if (_user is null)
                throw new InvalidOperationException("User state is not set");
            _user.Nickname = nickname;
        }

        public void SetAuthType(AuthType? authType)
        {
            if (_user is null)
                throw new InvalidOperationException("User state is not set");
            _user.AuthType = authType?.ToString();
        }

        public void SetWalletAddress(string? walletAddress)
        {
            if (_user is null)
                throw new InvalidOperationException("User state is not set");
            _user.WalletAddress = walletAddress;
        }

        public void ClearUser() => _user = null;

        public void SetRegion(string region)
        {
            if (_lobby?.Region != region)
                _lobby = new LobbyState(region);
        }

        public void ClearLobby() => _lobby = null;

        public void SetPlayPadVersion(string protocolVersion)
        {
            if (_playPad?.ProtocolVersion != protocolVersion)
                _playPad = new PlayPadState(protocolVersion);
        }

        public void SetCapabilities(string? capabilities)
        {
            if (_playPad is null)
                throw new InvalidOperationException("PlayPad state is not set");
            _playPad.Capabilities = capabilities;
        }

        public void SetFeatureAccess(string? featureAccess)
        {
            if (_playPad is null)
                throw new InvalidOperationException("PlayPad state is not set");
            _playPad.FeatureAccess = featureAccess;
        }

        public void SetTournamentId(string? tournamentId)
        {
            if (_playPad is null)
                throw new InvalidOperationException("PlayPad state is not set");
            _playPad.TournamentId = tournamentId;
        }

        public void ClearPlayPad() => _playPad = null;

        public void SetRoomId(string roomId)
        {
            if (_room?.RoomId != roomId)
                _room = new RoomState(roomId);
        }

        public void SetQueue(string? queueName)
        {
            if (_room is null)
                throw new InvalidOperationException("Room state is not set");
            _room.QueueName = queueName;
        }

        public void SetMatchId(string? matchId)
        {
            if (_room is null)
                throw new InvalidOperationException("Room state is not set");
            _room.MatchId = matchId;
        }

        public void ClearRoom() => _room = null;

        public void SetTcpUdp()
        {
            if (_gameServer is not TcpUdpState)
                _gameServer = new TcpUdpState();
        }

        public void SetWebRtc()
        {
            if (_gameServer is not WebRtcState)
                _gameServer = new WebRtcState();
        }

        public void SetGameServerAddress(string? tcpUdpAddress, string? webRtcAddress)
        {
            if (_gameServer is null)
                throw new InvalidOperationException("Game server state is not set");

            _gameServer.ServerAddress = _gameServer switch
            {
                TcpUdpState => tcpUdpAddress,
                WebRtcState => webRtcAddress,
                _ => throw new InvalidOperationException("Invalid game server state is set"),
            };
        }

        public void SetUsesTurn(bool usesTurn = true)
        {
            if (_gameServer is not WebRtcState webRtcState)
                throw new InvalidOperationException("WebRtc state is not set");
            webRtcState.UsesTurn = usesTurn;
        }

        public void ClearGameServer() => _gameServer = null;

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
