using System;
using Proto.ProtoClient;

namespace UnityConnectors.HalfRemote.Server
{
	internal interface IHalfRemoteGameEngineServer : IDisposable
	{
		event Action<Guid, GameEngineProtoClient> ReliableClientConnected;
		event Action<Guid, string>                ReliableClientReceivingError;
		event Action<Guid>                        ReliableClientReceivingEnded;

		event Action<Guid, GameEngineProtoClient> UnreliableClientConnected;
		event Action<Guid, string>                ClientConnectingError;
		event Action<Guid, string>                UnreliableClientReceivingError;
		event Action<Guid>                        UnreliableClientReceivingEnded;

		event Action<string, string> ListeningError;
		event Action<string>         ListeningEnded;

		void Start();
		void Stop();
	}
}
