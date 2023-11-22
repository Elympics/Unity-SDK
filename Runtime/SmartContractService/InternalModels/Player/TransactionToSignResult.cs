using System;

#nullable enable

namespace SCS.InternalModels.Player
{
    [Serializable]
    internal class TransactionToSignResult
    {
        public string From;
        public string To;
        public string Data;
    }
}
