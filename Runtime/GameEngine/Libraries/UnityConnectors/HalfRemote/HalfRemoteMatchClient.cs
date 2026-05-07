using System;
using Google.Protobuf;
using Proto.ProtoClient;
using Proto.ProtoClient.NetworkClient;
using Proto.ProtoClient.Receivers;
using ProtoGameEngine;
using ProtoLog;
using ProtoNtp;
using ProtoUnityGameEngine;
using UnityConnectors.HalfRemote.Ntp;
using UnityConnectors.HalfRemote.Server;
using WebRtcWrapper;

namespace UnityConnectors.HalfRemote
{
    internal class HalfRemoteMatchClient : ILogReceiver, IUnityGameEngineProtoReceiver, IClientNtpReceiver
    {
        public event Action<byte[], string> InGameDataForPlayerOnReliableChannelGenerated;
        public event Action<byte[], string> InGameDataForPlayerOnUnreliableChannelGenerated;
        public event Action<NtpData> NtpReceived;
        public event Action WebRtcUpgraded;

        public event Action<string> ReliableReceivingError;
        public event Action ReliableReceivingEnded;
        public event Action<string> UnreliableReceivingError;
        public event Action UnreliableReceivingEnded;

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

        public HalfRemoteMatchClient(string userId, IWebRtcClient webRtcClient)
            : this(userId,
                new ProtoNetworkDatagramClient(webRtcClient.AsReliableDatagramCommunication()),
                new ProtoNetworkDatagramClient(webRtcClient.AsUnreliableDatagramCommunication()))
        {
            webRtcClient.ReceiveWithThread();
        }

        public void PlayerConnected() => _reliableClient.Send(new PlayerConnectedMsg { UserId = _userId });
        public void PlayerDisconnected() => _reliableClient.Send(new PlayerDisconnectedMsg { UserId = _userId });
        public void SendInputReliable(byte[] data) => _reliableClient.Send(new InGameDataReliableReceivedMsg { Data = ByteString.CopyFrom(data), UserId = _userId });
        public void SendInputUnreliable(byte[] data) => _unreliableClient.Send(new InGameDataUnreliableReceivedMsg { Data = ByteString.CopyFrom(data), UserId = _userId });

        public void OnInGameDataForPlayerOnReliableChannelGenerated(InGameDataForPlayerOnReliableChannelGeneratedMsg message) => InGameDataForPlayerOnReliableChannelGenerated?.Invoke(message.Data.ToByteArray(), message.UserId);
        public void OnInGameDataForPlayerOnUnreliableChannelGenerated(InGameDataForPlayerOnUnreliableChannelGeneratedMsg message) => InGameDataForPlayerOnUnreliableChannelGenerated?.Invoke(message.Data.ToByteArray(), message.UserId);

        public void OnInGameDataForSpectatorsOnReliableChannelGenerated(InGameDataForSpectatorsOnReliableChannelGeneratedMsg message) => ThrowNotImplementedException();
        public void OnInGameDataForSpectatorsOnUnreliableChannelGenerated(InGameDataForSpectatorsOnUnreliableChannelGeneratedMsg message) => ThrowNotImplementedException();
        public void Initialized() => ThrowNotImplementedException();
        public void OnGameEnded(NullableGameEndedMsg message) => ThrowNotImplementedException();
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
