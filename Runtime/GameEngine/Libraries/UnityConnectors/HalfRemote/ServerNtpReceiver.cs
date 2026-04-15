using System;
using Google.Protobuf;
using Proto.ProtoClient;
using Proto.ProtoClient.Receivers;
using ProtoNtp;
using UnityConnectors.HalfRemote.Ntp;

namespace UnityConnectors.HalfRemote
{
	internal class ServerNtpReceiver : IServerNtpReceiver
	{
		public void OnNtp(ProtoClient client, NtpMsg ntpMsg)
		{
			var ntpRequest = new NtpData();
			ntpRequest.SetFromBytes(ntpMsg.Data.ToByteArray());

			var ntpResponse = new NtpData
			{
				OriginateTimestamp = ntpRequest.TransmitTimestamp,
				ReceiveTimestamp = DateTime.UtcNow,
				TransmitTimestamp = DateTime.UtcNow
			};

			client.Send(new NtpMsg
			{
				Data = ByteString.CopyFrom(ntpResponse.Data)
			});
		}
	}
}
