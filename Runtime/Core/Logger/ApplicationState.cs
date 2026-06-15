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
        private MatchState? _match;
        private GameServerState? _gameServer;
        private PlayPadState? _playPad;

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

        public void SetRegion(string? region) => _sdk.Region = region;

        public void SetPlayPad(string protocolVersion, string? capabilities = null, string? featureAccess = null) =>
            _playPad = new PlayPadState(protocolVersion)
            {
                Capabilities = capabilities,
                FeatureAccess = featureAccess
            };

        public void SetTournamentId(string? tournamentId)
        {
            if (_playPad is null)
                throw new InvalidOperationException("PlayPad state is not set");
            _playPad.TournamentId = tournamentId;
        }

        public void ClearPlayPad() => _playPad = null;

        public void SetRoomId(string? roomId) => (_match ??= new MatchState()).RoomId = roomId;

        public void SetQueue(string? queueName) => (_match ??= new MatchState()).QueueName = queueName;

        public void SetMatchId(string? matchId) => (_match ??= new MatchState()).MatchId = matchId;

        public void ClearRoom() => _match = null;

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

        public bool Visit(IStateVisitor visitor)
        {
            visitor.ProcessSubstate(nameof(SdkState));
            _ = _sdk.Visit(visitor);

            if (_game is not null)
            {
                visitor.ProcessSubstate(nameof(GameState));
                _ = _game.Visit(visitor);
            }

            if (_user is not null)
            {
                visitor.ProcessSubstate(nameof(UserState));
                _ = _user.Visit(visitor);
            }

            if (_match is not null)
            {
                visitor.ProcessSubstate(nameof(MatchState));
                _ = _match.Visit(visitor);
            }

            if (_gameServer is not null)
            {
                visitor.ProcessSubstate(nameof(GameServerState));
                _ = _gameServer.Visit(visitor);
            }

            if (_playPad is not null)
            {
                visitor.ProcessSubstate(nameof(PlayPadState));
                _ = _playPad.Visit(visitor);
            }

            return true;
        }
    }
}
