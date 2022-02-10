using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Elympics
{
	public class SimpleHttpSignalingClient
	{
		private readonly Uri _uri;

		private const int RequestTimeout = 5;

		public UnityWebRequest Request { get; private set; }

		public SimpleHttpSignalingClient(Uri uri)
		{
			_uri = uri;
		}

		public IEnumerator PostOfferAsync(string offer)
		{
			Request?.Abort();

			var rawOffer = Encoding.UTF8.GetBytes(offer);

			var signalingUri = new Uri(_uri, "/doSignaling");
			Debug.Log(signalingUri);
			Request = new UnityWebRequest(signalingUri, "POST")
			{
				timeout = RequestTimeout,
				uploadHandler = new UploadHandlerRaw(rawOffer) {contentType = "application/json"},
				downloadHandler = new DownloadHandlerBuffer()
			};

			yield return Request.SendWebRequest();
		}
	}
}
