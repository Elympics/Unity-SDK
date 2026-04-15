using ProtoNtp;

namespace Proto.ProtoClient.Receivers
{
	internal interface IServerNtpReceiver
	{
		void OnNtp(ProtoClient client, NtpMsg ntpMsg);
	}

	public interface IClientNtpReceiver
	{
		void OnNtp(NtpMsg ntpMsg);
	}
}
