using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace WebRtcWrapper
{
    public class WebRtcServer : IWebRtcServer
    {
        private static readonly TimeSpan ReceivePollInterval = TimeSpan.FromMilliseconds(5);

        private readonly int _port;
        private readonly string _publicIpOverride;
        private readonly int? _publicPortOverride;
        private readonly Guid _id;

        private CancellationTokenSource _receivingCts;
        private Thread _receivingThread;

        private readonly Dictionary<Guid, WebRtcServerClient> _clientsReliable = new();
        private readonly Dictionary<Guid, WebRtcServerClient> _clientsToAddReliable = new();
        private readonly Dictionary<Guid, WebRtcServerClient> _clientsToRemoveReliable = new();

        private readonly Dictionary<Guid, WebRtcServerClient> _clientsUnreliable = new();
        private readonly Dictionary<Guid, WebRtcServerClient> _clientsToAddUnreliable = new();
        private readonly Dictionary<Guid, WebRtcServerClient> _clientsToRemoveUnreliable = new();

        public WebRtcServer(int port, string publicIpOverride = null, int? publicPortOverride = null)
        {
            if (!string.IsNullOrEmpty(publicIpOverride))
            {
                var ipAddr = IPAddress.Parse(publicIpOverride).MapToIPv4();
                if (ipAddr.Equals(IPAddress.Loopback))
                    throw new ArgumentException("Forced IP for WebRTC should not be local!", nameof(publicIpOverride));
                if (ipAddr.Equals(IPAddress.Any))
                    throw new ArgumentException("Forced IP for WebRTC should not be zeros!", nameof(publicIpOverride));
            }

            _port = port;
            _publicIpOverride = publicIpOverride ?? "";
            _publicPortOverride = publicPortOverride;
            _id = WebRtcWrapper.ServerNew();
        }

        public void Start(bool withReceiveThread = true)
        {
            WebRtcWrapper.ServerStart(_id, _port, _publicIpOverride);
            if (withReceiveThread)
            {
                _receivingCts = new CancellationTokenSource();
                _receivingThread = new Thread(() =>
                {
                    while (!_receivingCts.IsCancellationRequested)
                    {
                        ReceiveReliableOnce();
                        ReceiveUnreliableOnce();

                        Thread.Sleep(ReceivePollInterval);
                    }
                });
                _receivingThread.Start();
            }
        }

        public void Stop()
        {
            WebRtcWrapper.ServerStop(_id);
            _receivingCts?.Cancel();
            _receivingThread?.Join();
        }

        public IWebRtcServerClient CreateClient()
        {
            return new WebRtcServerClient(this, _id, _publicPortOverride);
        }

        public void ReceiveReliableOnce()
        {
            ReceiveOnce(_clientsReliable, _clientsToAddReliable, _clientsToRemoveReliable, client => client.ReceiveReliableOnce(), true);
        }

        private static void ReceiveOnce(
            Dictionary<Guid, WebRtcServerClient> clients,
            Dictionary<Guid, WebRtcServerClient> clientsToAdd,
            Dictionary<Guid, WebRtcServerClient> clientsToRemove,
            Func<IWebRtcServerClient, bool> receiveFunc,
            bool shouldClose)
        {
            lock (clientsToAdd)
            {
                foreach (var client in clientsToAdd)
                    clients.Add(client.Key, client.Value);
                clientsToAdd.Clear();
            }

            foreach (var client in clients)
            {
                if (!receiveFunc(client.Value))
                    clientsToRemove.Add(client.Key, client.Value);
            }

            foreach (var client in clientsToRemove)
            {
                _ = clients.Remove(client.Key);
                if (shouldClose)
                    client.Value.Close();
            }

            clientsToRemove.Clear();
        }

        public void ReceiveUnreliableOnce()
        {
            ReceiveOnce(_clientsUnreliable, _clientsToAddUnreliable, _clientsToRemoveUnreliable, client => client.ReceiveUnreliableOnce(), false);
        }

        public void Dispose()
        {
            WebRtcWrapper.ServerRemove(_id);
        }

        internal void AddClientForReceivingReliable(Guid clientId, WebRtcServerClient webRtcServerClient)
        {
            lock (_clientsToAddReliable)
                _clientsToAddReliable.Add(clientId, webRtcServerClient);
        }

        internal void AddClientForReceivingUnreliable(Guid clientId, WebRtcServerClient webRtcServerClient)
        {
            lock (_clientsToAddUnreliable)
                _clientsToAddUnreliable.Add(clientId, webRtcServerClient);
        }
    }
}
