using System;
using System.Threading;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking;
using Elympics.Models.Matchmaking.WebSocket;
using HybridWebSocket;
using MessagePack;
using MatchmakingRoutes = Elympics.Models.Matchmaking.Routes;

#pragma warning disable CS0067

namespace Elympics
{
    internal class WebSocketMatchmakerClient : MatchmakerClient
    {
        private const string CanceledByUserCloseReason = "Canceled by user";

        private readonly string _findAndJoinMatchUrl;

        private IWebSocket _ws;
        private CancellationTokenRegistration _ctRegistration;
        private byte[] _serializedGameData;
        private byte[] _serializedJoinRequest;
        private Guid _gameId;
        private string _gameVersion;
        private Guid _matchId;

        private static readonly byte[] SerializedPong = MessagePackSerializer.Serialize<IToMatchmaker>(new Pong());

        private Action<IFromMatchmaker> _onMessage;

        internal WebSocketMatchmakerClient(string baseUrl) : base(baseUrl)
        {
            var uriBuilder = new UriBuilder(baseUrl);
            var oldPath = uriBuilder.Path.TrimEnd('/');
            uriBuilder.Path = string.Join("/", oldPath, MatchmakingRoutes.Base, MatchmakingRoutes.FindAndJoinMatch);
            _findAndJoinMatchUrl = uriBuilder.Uri.ToString();
        }

        private void SetUp(string jwtToken, CancellationToken ct)
        {
            EmitMatchmakingStarted(_gameId, _gameVersion);
            _ctRegistration = ct.Register(CancelConnection);

            var (url, protocol) = _findAndJoinMatchUrl.ToWebSocketAddress(jwtToken);
            _ws = WebSocketFactory.CreateInstance(url, protocol);
            _ws.OnOpen += SendJoinRequest;
            _ws.OnMessage += HandleMessage;
            _ws.OnClose += HandleClose;
            _ws.OnError += HandleError;
            _ws.Connect();
        }

        protected override void JoinMatchmakerInternal(JoinMatchmakerData joinMatchmakerData, AuthData authData, CancellationToken ct = default)
        {
            if (_ws != null)
                CancelConnection();

            _serializedGameData = MessagePackSerializer.Serialize<IToMatchmaker>(new GameData(ElympicsConfig.SdkVersion, joinMatchmakerData));
            _serializedJoinRequest = MessagePackSerializer.Serialize<IToMatchmaker>(new JoinMatchmaker(joinMatchmakerData));
            _gameId = joinMatchmakerData.GameId;
            _gameVersion = joinMatchmakerData.GameVersion;

            SetUp(authData.JwtToken, ct);
        }

        private void SendJoinRequest()
        {
            _ws.OnOpen -= SendJoinRequest;

            _onMessage = ReceiveMatchId;
            _ws.Send(_serializedGameData);
            _ws.Send(_serializedJoinRequest);
        }

        private void ReceiveMatchId(IFromMatchmaker message)
        {
            if (message is not MatchFound matchFound)
                return;

            _onMessage = ReceiveMatchReady;
            _matchId = matchFound.MatchId;
            EmitMatchmakingMatchFound(_matchId, _gameId, _gameVersion);
        }

        private void ReceiveMatchReady(IFromMatchmaker message)
        {
            if (message is not MatchData matchData)
                return;

            _onMessage = null;
            _ = CleanUp();
            EmitMatchmakingSucceeded(new MatchmakingFinishedData(matchData));
        }

        private void CancelConnection()
        {
            var matchId = CleanUp((WebSocketCloseCode.Normal, CanceledByUserCloseReason));
            EmitMatchmakingCancelled(matchId, _gameId, _gameVersion);
        }

        private void HandleMessage(byte[] data)
        {
            IFromMatchmaker messageFromMatchmaker;
            try
            {
                messageFromMatchmaker = MessagePackSerializer.Deserialize<IFromMatchmaker>(data);
            }
            catch (Exception exception)
            {
                HandleWarning($"Could not deserialize matchmaker response\n{exception}");
                return;
            }

            switch (messageFromMatchmaker)
            {
                case Ping:
                    _ws.Send(SerializedPong);
                    break;
                case MatchmakingError matchmakingError:
                    HandleError($"[{matchmakingError.StatusCode}] {matchmakingError.ErrorBlame}");
                    break;
                default:
                    _onMessage?.Invoke(messageFromMatchmaker);
                    break;
            }
        }

        private void HandleClose(WebSocketCloseCode code, string reason)
        {
            var matchId = CleanUp();
            if (code != WebSocketCloseCode.Normal)
                EmitMatchmakingFailed($"Connection closed abnormally [{code}] {reason}", matchId, _gameId, _gameVersion);
        }

        private void HandleWarning(string message)
        {
            EmitMatchmakingWarning(message, _matchId);
        }

        private void HandleError(string message)
        {
            var matchId = CleanUp((WebSocketCloseCode.Abnormal, message));
            EmitMatchmakingFailed(message, matchId, _gameId, _gameVersion);
        }

        /// <summary>
        /// Resets the state of RemoteMatchmakerClient instance, unregistering all callbacks and cleaning up internal data.
        /// </summary>
        /// <param name="closeArgs">Optional arguments to pass to WebSocket closing method. If omitted, the socket isn't closed.</param>
        /// <returns>The value of <see cref="_matchId"/> from before the clean-up</returns>
        private Guid CleanUp((WebSocketCloseCode Code, string Reason)? closeArgs = null)
        {
            _ctRegistration.Dispose();
            UnregisterCallbacks();
            var ws = _ws;
            var matchId = _matchId;
            _ws = null;
            _matchId = Guid.Empty;
            _gameId = Guid.Empty;
            _gameVersion = null;
            if (closeArgs.HasValue)
                try
                {
                    ws.Close(closeArgs.Value.Code, closeArgs.Value.Reason);
                }
                catch { /* ignored */ }
            return matchId;
        }

        private void UnregisterCallbacks()
        {
            _ws.OnOpen -= SendJoinRequest;
            _ws.OnMessage -= HandleMessage;
            _ws.OnClose -= HandleClose;
            _ws.OnError -= HandleError;
            _onMessage = null;
        }
    }
}
