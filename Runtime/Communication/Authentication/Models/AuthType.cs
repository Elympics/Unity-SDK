using System;

namespace Elympics.Models.Authentication
{
	[Serializable]
	public enum AuthType
	{
		Unknown = 0,
		ClientSecret = 1,
		EthAddress = 2,
	}
}
