using System;

namespace Elympics.Models.Authentication
{
    [Serializable]
    public enum AuthType
    {
        None = 0,
        ClientSecret = 1,
        EthAddress = 2,
        Telegram = 3,
    }
}
