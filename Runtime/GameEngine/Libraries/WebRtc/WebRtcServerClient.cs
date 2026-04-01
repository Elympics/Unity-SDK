using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebRtcWrapper
{
    public class WebRtcServerClient : IWebRtcServerClient
    {
        public event Action<byte[]> ReliableReceived;
        public event Action<string> ReliableReceivingError;
        public event Action ReliableReceivingEnded;

        public event Action<byte[]> UnreliableReceived;
        public event Action<string> UnreliableReceivingError;
        public event Action UnreliableReceivingEnded;
        public event Action<string>? IceConnectionStateChanged;
        public event Action<string>? ConnectionStateChanged;

        private readonly WebRtcServer _webRtcServer;
        private readonly Guid _serverId;
        private readonly int? _forwardedPortOverride;
        private Guid _clientId;

        // Keep references to prevent garbage collection
        private WebRtcWrapper.ReceivedDelegate _reliableReceivedDelegate;
        private WebRtcWrapper.ReceivingErrorDelegate _reliableErrorDelegate;
        private WebRtcWrapper.ReceivingEndedDelegate _reliableEndedDelegate;
        private WebRtcWrapper.ReceivedDelegate _unreliableReceivedDelegate;
        private WebRtcWrapper.ReceivingErrorDelegate _unreliableErrorDelegate;
        private WebRtcWrapper.ReceivingEndedDelegate _unreliableEndedDelegate;

        public WebRtcServerClient(WebRtcServer webRtcServer, Guid serverId, int? forwardedPortOverride = null)
        {
            _webRtcServer = webRtcServer;
            _serverId = serverId;
            _forwardedPortOverride = forwardedPortOverride;
        }

        public void Close()
        {
            if (_clientId == default)
                return;

            var clientId = _clientId;
            _clientId = default;
            WebRtcWrapper.ServerClientClose(_serverId, clientId);
            WebRtcWrapper.ServerRemoveClient(_serverId, clientId);
        }

        public void SendReliable(byte[] data)
        {
            if (_clientId != default)
                WebRtcWrapper.ServerClientSendReliable(_serverId, _clientId, data);
        }

        public void SendUnreliable(byte[] data)
        {
            if (_clientId != default)
                WebRtcWrapper.ServerClientSendUnreliable(_serverId, _clientId, data);
        }

        public void ReceiveReliable()
        {
            if (_clientId != default)
                _webRtcServer.AddClientForReceivingReliable(_clientId, this);
        }

        public void ReceiveUnreliable()
        {
            if (_clientId != default)
                _webRtcServer.AddClientForReceivingUnreliable(_clientId, this);
        }

        public bool ReceiveReliableOnce()
        {
            return _clientId != default && WebRtcWrapper.ServerClientReceiveReliable(_serverId, _clientId);
        }

        public bool ReceiveUnreliableOnce()
        {
            return _clientId != default && WebRtcWrapper.ServerClientReceiveUnreliable(_serverId, _clientId);
        }

        public async Task<string> CreateAnswerAsync(string offerJson)
        {
            string answer = null;
            string error = null;

            _reliableReceivedDelegate = WebRtcWrapper.CreateReceivedDelegate(OnReliableReceived);
            _reliableErrorDelegate = WebRtcWrapper.CreateReceivingErrorDelegate(OnReliableReceivingError);
            _reliableEndedDelegate = WebRtcWrapper.CreateReceivingEndedDelegate(OnReliableReceivingEnded);

            _unreliableReceivedDelegate = WebRtcWrapper.CreateReceivedDelegate(OnUnreliableReceived);
            _unreliableErrorDelegate = WebRtcWrapper.CreateReceivingErrorDelegate(OnUnreliableReceivingError);
            _unreliableEndedDelegate = WebRtcWrapper.CreateReceivingEndedDelegate(OnUnreliableReceivingEnded);

            await Task.Run(() => WebRtcWrapper.ServerNewClient(
                _serverId,
                offerJson,
                _reliableReceivedDelegate,
                _reliableErrorDelegate,
                _reliableEndedDelegate,
                _unreliableReceivedDelegate,
                _unreliableErrorDelegate,
                _unreliableEndedDelegate)
            ).ContinueWith(t =>
            {
                _clientId = t.Result.clientId;
                answer = t.Result.answerJson;
                error = t.Result.error;
            });

            if (error != null)
                throw new WebRtcException(error);

            if (_forwardedPortOverride.HasValue)
            {
                var regex = new Regex($"[0-9]+ typ host");
                answer = regex.Replace(answer, $"{_forwardedPortOverride.Value} typ host");
            }

            return answer;
        }

        private void OnReliableReceived(byte[] data) => ReliableReceived?.Invoke(data);
        private void OnReliableReceivingError(string error) => ReliableReceivingError?.Invoke(error);
        private void OnReliableReceivingEnded() => ReliableReceivingEnded?.Invoke();

        private void OnUnreliableReceived(byte[] data) => UnreliableReceived?.Invoke(data);
        private void OnUnreliableReceivingError(string error) => UnreliableReceivingError?.Invoke(error);
        private void OnUnreliableReceivingEnded() => UnreliableReceivingEnded?.Invoke();
    }
}
