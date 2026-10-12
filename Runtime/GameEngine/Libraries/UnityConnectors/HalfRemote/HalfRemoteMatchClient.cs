using System;
using System.Linq;
using Google.Protobuf;
using MatchTcpLibrary.TransportLayer.Interfaces;
using Proto.ProtoClient;
using Proto.ProtoClient.NetworkClient;
using Proto.ProtoClient.Receivers;
using ProtoGameEngine;
using ProtoLog;
using ProtoNtp;
using ProtoUnityGameEngine;
using UnityConnectors.HalfRemote.Ntp;

namespace UnityConnectors.HalfRemote
{
    internal class HalfRemoteMatchClient : ILogReceiver, IUnityGameEngineProtoReceiver, IClientNtpReceiver
    {
        public event Action<byte[], string> InGameDataForPlayerOnReliableChannelGenerated;
        public event Action<byte[], string> InGameDataForPlayerOnUnreliableChannelGenerated;
        public event Action<NtpData> NtpReceived;

        public event Action<string> ReliableReceivingError;
        public event Action ReliableReceivingEnded;
        public event Action<string> UnreliableReceivingError;
        public event Action UnreliableReceivingEnded;

        public event Action<Guid> MatchEnded;

        private readonly string _userId;
        private readonly UnityGameEngineProtoClient _reliableClient;
        private readonly UnityGameEngineProtoClient _unreliableClient;

        public HalfRemoteMatchClient(string userId, IProtoNetworkClient reliableClient, IProtoNetworkClient unreliableClient = null)
        {
            _userId = userId;

            _reliableClient = new UnityGameEngineProtoClient(reliableClient, this, this, this);
            _reliableClient.ReceivingError += error => ReliableReceivingError?.Invoke(error);
            _reliableClient.ReceivingEnded += () => ReliableReceivingEnded?.Invoke();
            _reliableClient.Receive();

            _unreliableClient = unreliableClient != null
                ? new UnityGameEngineProtoClient(unreliableClient, this, this, this)
                : _reliableClient;

            _unreliableClient.ReceivingError += error => UnreliableReceivingError?.Invoke(error);
            _unreliableClient.ReceivingEnded += () => UnreliableReceivingEnded?.Invoke();

            if (unreliableClient != null)
                _unreliableClient.Receive();
        }

        public HalfRemoteMatchClient(string userId, IDataChannel reliableChannel, IDataChannel unreliableChannel)
            : this(userId,
                new ProtoNetworkDatagramClient(new DataChannelDatagramAdapter(reliableChannel)),
                new ProtoNetworkDatagramClient(new DataChannelDatagramAdapter(unreliableChannel)))
        { }

        public void PlayerConnected() => _reliableClient.Send(new PlayerConnectedMsg { UserId = _userId });
        public void PlayerDisconnected() => _reliableClient.Send(new PlayerDisconnectedMsg { UserId = _userId });
        public void SendInputReliable(byte[] data) => _reliableClient.Send(new InGameDataReliableReceivedMsg { Data = ByteString.CopyFrom(data), UserId = _userId });
        public void SendInputUnreliable(byte[] data) => _unreliableClient.Send(new InGameDataUnreliableReceivedMsg { Data = ByteString.CopyFrom(data), UserId = _userId });

        public void OnInGameDataForPlayerOnReliableChannelGenerated(InGameDataForPlayerOnReliableChannelGeneratedMsg message) => InGameDataForPlayerOnReliableChannelGenerated?.Invoke(message.Data.ToByteArray(), message.UserId);
        public void OnInGameDataForPlayerOnUnreliableChannelGenerated(InGameDataForPlayerOnUnreliableChannelGeneratedMsg message) => InGameDataForPlayerOnUnreliableChannelGenerated?.Invoke(message.Data.ToByteArray(), message.UserId);

        public void OnInGameDataForSpectatorsOnReliableChannelGenerated(InGameDataForSpectatorsOnReliableChannelGeneratedMsg message) => ThrowNotImplementedException();
        public void OnInGameDataForSpectatorsOnUnreliableChannelGenerated(InGameDataForSpectatorsOnUnreliableChannelGeneratedMsg message) => ThrowNotImplementedException();
        public void Initialized() => ThrowNotImplementedException();

        public void OnGameEnded(NullableGameEndedMsg message) =>
            MatchEnded?.Invoke(message.HasNull ? Guid.Empty : new Guid(message.Data.Data.First().UserId.Split('.')[0]));

        public void LogVerbose(LogVerboseMsg message) => ThrowNotImplementedException();
        public void LogDebug(LogDebugMsg message) => ThrowNotImplementedException();
        public void LogInfo(LogInfoMsg message) => ThrowNotImplementedException();
        public void LogWarning(LogWarningMsg message) => ThrowNotImplementedException();
        public void LogError(LogErrorMsg message) => ThrowNotImplementedException();
        public void LogFatal(LogFatalMsg msg) => ThrowNotImplementedException();

        private static void ThrowNotImplementedException() => throw new NotImplementedException("This message is not supported in half-remote mode");

        public void SendNtp()
        {
            var ntpData = new NtpData
            {
                TransmitTimestamp = DateTime.UtcNow
            };
            _reliableClient?.Send(new NtpMsg
            {
                Data = ByteString.CopyFrom(ntpData.Data)
            });
        }

        public void OnNtp(NtpMsg ntpMsg)
        {
            var ntpData = new NtpData();
            ntpData.SetFromBytes(ntpMsg.Data.ToByteArray());
            ntpData.ReceptionTimestamp = DateTime.UtcNow;
            NtpReceived?.Invoke(ntpData);
        }
    }
}
