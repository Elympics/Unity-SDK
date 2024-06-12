using System.Numerics;

#nullable enable

namespace SCS
{
    public class DepositState
    {
        public string TokenAddress { get; set; } = default!;
        public BigInteger ActualAmount { get; set; }
        public BigInteger AvailableAmount { get; set; }

        public DepositState()
        { }

        internal DepositState(InternalModels.Player.DepositState depositState)
        {
            TokenAddress = depositState.TokenAddress;
            var totalAmount = BigInteger.Parse(depositState.AmountTotal);
            ActualAmount = totalAmount;
            var lockedForSettlement = BigInteger.Parse(depositState.LockedPendingSettlement);
            var lockedForWithdrawal = BigInteger.Parse(depositState.LockedPendingWithdrawal);
            AvailableAmount = totalAmount - lockedForSettlement - lockedForWithdrawal;
        }
    }
}
