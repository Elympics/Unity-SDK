using System;

namespace MatchTcpLibrary.Ntp
{
	public class NtpException : Exception
	{
		public NtpException(string s) : base(s)
		{
		}
	}
}
