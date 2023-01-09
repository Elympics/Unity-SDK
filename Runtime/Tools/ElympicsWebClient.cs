using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Elympics
{
	public static class ElympicsWebClient
	{
		public static string SdkVersion { get; } = ElympicsConfig.Load().ElympicsVersion;

		public static UnityWebRequestAsyncOperation SendJsonPostRequest(string url, object body, string auth)
		{
			var uri = new Uri(url);
			var request = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPOST);
			var bodyString = JsonUtility.ToJson(body);
			var bodyRaw = Encoding.ASCII.GetBytes(bodyString);
			request.uploadHandler = new UploadHandlerRaw(bodyRaw);
			request.downloadHandler = new DownloadHandlerBuffer();

			if (!string.IsNullOrEmpty(auth))
				request.SetRequestHeader("Authorization", auth);
			request.SetRequestHeader("Content-Type", "application/json");
			request.SetRequestHeader("Accept", "application/json");
			request.SetRequestHeader("elympics-sdk-version", SdkVersion);

			AcceptTestCertificateHandler.SetOnRequestIfNeeded(request);

			Debug.Log($"[Elympics] Sending request POST {url}\n{bodyString}");
			return request.SendWebRequest();
		}

		public class AcceptTestCertificateHandler : CertificateHandler
		{
			private const string TestDomain = ".test";
			private const string VagrantTestHostPart = "vagrant";

			public static void SetOnRequestIfNeeded(UnityWebRequest request)
			{
				if (!request.uri.Host.Contains(TestDomain) && !request.uri.Host.Contains(VagrantTestHostPart))
					return;

				Debug.Log($"Test domain cert handler set for domain {request.uri.Host}");
				request.certificateHandler = new AcceptTestCertificateHandler();
			}

			protected override bool ValidateCertificate(byte[] certificateData)
			{
				var certificate = new X509Certificate2(certificateData);
				return certificate.Subject.ToLower().Contains(TestDomain);
			}
		}
	}
}
