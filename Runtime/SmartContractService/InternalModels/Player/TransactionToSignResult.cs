#nullable enable

using System;

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
