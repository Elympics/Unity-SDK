using System;

#nullable enable

namespace SCS.InternalModels.Player
{
    [Serializable]
    internal class GetTicketResponse
    {
        public string Nonce;
        public string TypedData;

        public GetTicketResponse(string nonce, string typedData)
        {
            Nonce = nonce;
            TypedData = typedData;
        }
    }
}
