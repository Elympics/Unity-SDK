#nullable enable
using System;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MatchTcpLibrary.TransportLayer.Interfaces
{
    internal interface IDataChannel : IDisposable
    {
        string Label { get; }

        bool IsConnected { get; }
        event Action? Disconnected;
        event Action<byte[]>? DataReceived;
        event Action<string>? Error;

        void CreateAndBind();
        UniTask ConnectAsync(IPEndPoint remoteEndPoint, CancellationToken ct = default);
        void Send(byte[] payload);
        void Disconnect();
    }
}
