using System;
using System.Threading;

namespace MatchTcpLibrary
{
	public interface IGameServerWebSignalingClient
	{
		event Action<WebSignalingClientResponse> ReceivedResponse;

		void PostOfferAsync(string offer, int timeoutSeconds, CancellationToken ct = default);
	}

	public class WebSignalingClientResponse
	{
		public bool   IsError { get; set; }
		public string Text    { get; set; }
	}
}
