using System;

namespace Elympics.Models.Authentication
{
    [Serializable]
    public struct EthAddressAuthRequest
    {
        public string address;
        public string msg;
        public string sig;
    }
}
