using System.Collections.Generic;

namespace MatchTcpLibrary.TransportLayer.Interfaces
{
	public interface IMessageEncoder
	{
		byte[] EncodePayload(byte[] payload);
		List<List<byte>> ExtractCompleteMessages(List<byte> payloadQueue);
	}
}