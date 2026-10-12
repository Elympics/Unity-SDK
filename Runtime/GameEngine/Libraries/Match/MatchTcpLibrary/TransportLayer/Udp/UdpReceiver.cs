#nullable enable
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MatchTcpLibrary.TransportLayer.Udp
{
    public class UdpReceiver
    {
        private CancellationTokenSource? _cts;
        private readonly UdpClient _udpClient;

        public event Action? ReceivingStopped;
        public event Action<byte[], IPEndPoint, DateTime>? DataReceived;

        public UdpReceiver(UdpClient udpClient) => _udpClient = udpClient;

        public async UniTaskVoid StartReceiving()
        {
            _cts = new CancellationTokenSource();
            while (!_cts.IsCancellationRequested)
                try
                {
                    var result = await _udpClient.ReceiveAsync().AsUniTask();
                    var receiveTime = DateTime.UtcNow;
                    DataReceived?.Invoke(result.Buffer, result.RemoteEndPoint, receiveTime);
                }
                catch (Exception)
                {
                    break;
                }

            ReceivingStopped?.Invoke();
        }

        public void StopReceiving() => _cts.Cancel();
    }
}
