#nullable enable
using System;
using Elympics.Core.Logger.State;

namespace Elympics.Core.Logger
{
    [Serializable]
    internal class ApplicationState : IVisitableState
    {
        private SdkState _sdk;
        private GameState? _game;
        private UserState? _user;
        private LobbyState? _lobby;
        private PlayPadState? _playPad;
        private RoomState? _room;
        private GameServerState? _gameServer;

        public ApplicationState(string sdkVersion) => _sdk = new SdkState(sdkVersion);

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

        public ApplicationState SetRegion(string region)
        {
            if (_lobby is null)
                _lobby = new LobbyState(region);
            else
                _lobby.Region = region;
            return this;
        }

        public ApplicationState SetNoConnection()
        {
            _lobby = null;
            return this;
        }

        public ApplicationState SetUserId(string userId)
        {
            if (_user is null)
                _user = new UserState(userId);
            else
                _user.UserId = userId;
            return this;
        }

        public ApplicationState SetNickname(string nickname)
        {
            if (_user is null)
                throw new InvalidOperationException("User state is not set");
            _user.Nickname = nickname;
            return this;
        }

        public ApplicationState SetAuthType(string authType)
        {
            if (_user is null)
                throw new InvalidOperationException("User state is not set");
            _user.AuthType = authType;
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

        public ApplicationState SetProtocolVersion(string protocolVersion)
        {
            if (_playPad is null)
                _playPad = new PlayPadState(protocolVersion);
            else
                _playPad.ProtocolVersion = protocolVersion;
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
            if (_room is null)
                _room = new RoomState(roomId);
            else
               _room.RoomId = roomId;
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
            if (_gameServer is not TcpUdpState)
                _gameServer = new TcpUdpState(serverAddress);
            else
                _gameServer.ServerAddress = serverAddress;
            return this;
        }

        public ApplicationState SetWebRtcServerAddress(string serverAddress)
        {
            if (_gameServer is not WebRtcState)
                _gameServer = new WebRtcState(serverAddress);
            else
                _gameServer.ServerAddress = serverAddress;
            return this;
        }

        public ApplicationState SetSdkConfiguration(string sdkVersion, string apiUrl, string gameServerUrl)
        {
            _sdk.SdkVersion = sdkVersion;
            _sdk.ApiUrl = apiUrl;
            _sdk.GameServerUrl = gameServerUrl;
            return this;
        }

        public ApplicationState SetPlayPadSdkContext(string protocolVersion)
        {
            if (_playPad is null)
                _playPad = new PlayPadState(protocolVersion);
            else
                _playPad.ProtocolVersion = protocolVersion;
            return this;
        }

        public ApplicationState SetFleetName(string fleetName)
        {
            _game.FleetName = fleetName;
            return this;
        }

        public ApplicationState SetGameVersionId(string gameVersionId)
        {
            _game.GameVersionId = gameVersionId;
            return this;
        }

        public ApplicationState SetGameMode(string gameMode)
        {
            _game.GameMode = gameMode;
            return this;
        }

        public ApplicationState SetUsesTurn(bool usesTurn = true)
        {
            if (_gameServer is not WebRtcState webRtcState)
                throw new InvalidOperationException("WebRtc state is not set");
            webRtcState.UsesTurn = usesTurn;
            return this;
        }
    }
}
