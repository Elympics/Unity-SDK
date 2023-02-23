using System;
using System.Threading;

namespace MatchTcpLibrary
{
	public interface IGameServerWebSignalingClient
	{
		event Action<WebSignalingResponse> ReceivedResponse;

		void PostOfferAsync(string offer, int timeoutSeconds, CancellationToken ct = default);
	}

	public class WebSignalingResponse
	{
		public bool IsError { get; set; }
		public string Text { get; set; }
	}
}
