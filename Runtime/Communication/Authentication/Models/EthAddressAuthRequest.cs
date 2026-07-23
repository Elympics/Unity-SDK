#nullable enable

using System;

namespace Elympics.Models.Authentication
{
    [Serializable]
    public struct EthAddressAuthRequest
    {
        public string typedData;
        public string signature;
    }
}
