using System;

namespace SCS.InternalModels.Player
{
    [Serializable]
    public class DepositState
    {
        public string TokenAddress;
        public string AmountTotal;
        public string LockedPendingSettlement;
        public string LockedPendingWithdrawal;
    }
}
