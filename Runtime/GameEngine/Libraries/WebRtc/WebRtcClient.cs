using System;
using System.Threading;

namespace WebRtcWrapper
{
    public class WebRtcClient : IWebRtcClient
    {
        public event Action<byte[]> ReliableReceived;
        public event Action<string> ReliableReceivingError;
        public event Action ReliableReceivingEnded;

        public event Action<byte[]> UnreliableReceived;
        public event Action<string> UnreliableReceivingError;
        public event Action UnreliableReceivingEnded;
        public event Action<string>? IceConnectionStateChanged;
        public event Action<string>? ConnectionStateChanged;

        public event Action<string> OfferCreated;
        public event Action<string>? IceCandidateCreated;

        private Guid _id;

        private CancellationTokenSource _receivingCts;
        private Thread _receivingThread;

        // Keep references to prevent garbage collection
        private readonly WebRtcWrapper.ReceivedDelegate _reliableReceivedDelegate;
        private readonly WebRtcWrapper.ReceivingErrorDelegate _reliableErrorDelegate;
        private readonly WebRtcWrapper.ReceivingEndedDelegate _reliableEndedDelegate;
        private readonly WebRtcWrapper.ReceivedDelegate _unreliableReceivedDelegate;
        private readonly WebRtcWrapper.ReceivingErrorDelegate _unreliableErrorDelegate;
        private readonly WebRtcWrapper.ReceivingEndedDelegate _unreliableEndedDelegate;
        private readonly WebRtcWrapper.OfferDelegate _offerDelegate;

        public WebRtcClient()
        {
            _reliableReceivedDelegate = WebRtcWrapper.CreateReceivedDelegate(OnReliableReceived);
            _reliableErrorDelegate = WebRtcWrapper.CreateReceivingErrorDelegate(OnReliableReceivingError);
            _reliableEndedDelegate = WebRtcWrapper.CreateReceivingEndedDelegate(OnReliableReceivingEnded);

            _unreliableReceivedDelegate = WebRtcWrapper.CreateReceivedDelegate(OnUnreliableReceived);
            _unreliableErrorDelegate = WebRtcWrapper.CreateReceivingErrorDelegate(OnUnreliableReceivingError);
            _unreliableEndedDelegate = WebRtcWrapper.CreateReceivingEndedDelegate(OnUnreliableReceivingEnded);

            _offerDelegate = WebRtcWrapper.CreateOfferDelegate(OnOffer);

            _id = WebRtcWrapper.ClientNew(
                _reliableReceivedDelegate,
                _reliableErrorDelegate,
                _reliableEndedDelegate,
                _unreliableReceivedDelegate,
                _unreliableErrorDelegate,
                _unreliableEndedDelegate);

            if (!ClientExists)
            {
                _reliableReceivedDelegate = null;
                _reliableErrorDelegate = null;
                _reliableEndedDelegate = null;
                _unreliableReceivedDelegate = null;
                _unreliableErrorDelegate = null;
                _unreliableEndedDelegate = null;
                _offerDelegate = null;
            }
        }

        private bool ClientExists => _id != default;

        public void Close()
        {
            _receivingCts?.Cancel();
            _receivingThread?.Join();

            if (ClientExists)
                WebRtcWrapper.ClientClose(_id);
        }

        public void ReceiveWithThread()
        {
            _receivingCts = new CancellationTokenSource();
            _receivingThread = new Thread(() =>
            {
                while (!_receivingCts.IsCancellationRequested)
                {
                    _ = ReceiveReliableOnce();
                    _ = ReceiveUnreliableOnce();
                    // Thread.Sleep(1) resolution in Windows is 15 ms (64 ticks per second) - don't use for real-time polling
                    _ = Thread.Yield();
                }
            });
            _receivingThread.Start();
        }

        public bool ReceiveReliableOnce()
        {
            return WebRtcWrapper.ClientReceiveReliable(_id);
        }

        public bool ReceiveUnreliableOnce()
        {
            return WebRtcWrapper.ClientReceiveUnreliable(_id);
        }

        private void OnReliableReceived(byte[] data) => ReliableReceived?.Invoke(data);
        private void OnReliableReceivingError(string error) => ReliableReceivingError?.Invoke(error);
        private void OnReliableReceivingEnded() => ReliableReceivingEnded?.Invoke();

        private void OnUnreliableReceived(byte[] data) => UnreliableReceived?.Invoke(data);
        private void OnUnreliableReceivingError(string error) => UnreliableReceivingError?.Invoke(error);
        private void OnUnreliableReceivingEnded() => UnreliableReceivingEnded?.Invoke();

        private void OnOffer(string offer) => OfferCreated?.Invoke(offer);

        private void OnIceCandidateCreated(string candidate) => IceCandidateCreated?.Invoke(candidate);

        public void SetIceServers(string iceServersJson)
        {
            // ICE servers are not used in the native WebRTC client
        }

        public void CreateOffer(bool restart)
        {
            if (ClientExists)
                WebRtcWrapper.ClientCreateOffer(_id, _offerDelegate);
        }

        public void OnAnswer(string answerJson)
        {
            if (ClientExists)
                WebRtcWrapper.ClientOnAnswer(_id, answerJson);
        }

        public void SendReliable(byte[] data)
        {
            if (ClientExists)
                WebRtcWrapper.ClientSendReliable(_id, data);
        }

        public void SendUnreliable(byte[] data)
        {
            if (ClientExists)
                WebRtcWrapper.ClientSendUnreliable(_id, data);
        }

        public void Dispose()
        {
            if (!ClientExists)
                return;

            Close();
            WebRtcWrapper.ClientRemove(_id);
            _id = default;
        }
    }
}
