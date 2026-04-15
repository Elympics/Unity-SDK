using System;

namespace UnityConnectors.HalfRemote.Ntp
{
	public class NtpException : Exception
	{
		public NtpException(string s) : base(s)
		{
		}
	}
}
