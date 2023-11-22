using System;

#nullable enable

namespace SCS.InternalModels.Player
{
    [Serializable]
    internal struct SendSignedTicketRequest
    {
        public string Nonce;
        public string Signature;

        public SendSignedTicketRequest(string nonce, string signature)
        {
            Nonce = nonce;
            Signature = signature;
        }
    }
}
