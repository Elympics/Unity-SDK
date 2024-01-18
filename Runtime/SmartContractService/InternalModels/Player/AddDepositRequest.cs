using System;

#nullable enable

namespace SCS.InternalModels.Player
{
    [Serializable]
    internal struct AddDepositRequest
    {
        public string GameId;
        public string Amount;
    }
}
