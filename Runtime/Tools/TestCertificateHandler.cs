using System.Security.Cryptography.X509Certificates;
using UnityEngine.Networking;

namespace Elympics
{
	public class TestCertificateHandler : CertificateHandler
	{
		public const string TestDomain = ".test";
		public const string SecureScheme = "https";

		protected override bool ValidateCertificate(byte[] certificateData)
		{
			var certificate = new X509Certificate2(certificateData);
			return certificate.Subject.ToLower().Contains(TestDomain);
		}
	}
}
