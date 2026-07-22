#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MatchTcpLibrary.TransportLayer.Interfaces
{
    internal interface INetworkClient : IDisposable
    {
        const string ReliableLabel = "reliable";
        const string UnreliableLabel = "unreliable";

        event Action? Connected;
        event Action<string>? ChannelOpened;
        event Action<string, byte[]>? DataReceived;
        event Action? Disconnected;

        bool IsConnected { get; }
        IReadOnlyDictionary<string, IDataChannel> Channels { get; }

        IDataChannel ReliableChannel => Channels[ReliableLabel];
        IDataChannel UnreliableChannel => Channels[UnreliableLabel];

        void CreateAndBind();
        UniTask Connect(CancellationToken ct = default);
        void Disconnect();
    }
}
