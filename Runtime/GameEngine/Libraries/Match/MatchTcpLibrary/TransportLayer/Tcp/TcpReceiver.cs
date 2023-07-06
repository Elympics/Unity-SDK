using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MatchTcpLibrary.TransportLayer.Tcp
{
    public class TcpReceiver
    {
        private CancellationTokenSource _cts;
        private readonly TcpClient _tcpClient;
        private readonly TcpProtocolConfig _tcpProtocolConfig;

        public event Action ReceivingStopped;
        public event Action<byte[]> DataReceived;

        public TcpReceiver(TcpClient tcpClient, TcpProtocolConfig tcpProtocolConfig)
        {
            _tcpClient = tcpClient;
            _tcpProtocolConfig = tcpProtocolConfig;
        }

        public async Task StartReceiving()
        {
            if (!_tcpClient.Connected)
                return;

            _cts = new CancellationTokenSource();

            var buffer = new byte[_tcpProtocolConfig.ReceiveBufferSize];
            using var netStream = _tcpClient.GetStream();
            int readBytes;
            while (ReadingNotFinished(readBytes = await netStream.ReadAsync(buffer, 0, buffer.Length, _cts.Token)))
            {
                DataReceived?.Invoke(buffer.Take(readBytes).ToArray());
            }

            ReceivingStopped?.Invoke();
        }

        public void StopReceiving()
        {
            _cts?.Cancel();
        }

        private static bool ReadingNotFinished(int readBytes)
        {
            return readBytes > 0;
        }
    }
}
