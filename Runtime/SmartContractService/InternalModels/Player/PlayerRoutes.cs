namespace SCS.InternalModels.Player
{
    public static class PlayerRoutes
    {
        public const string Base = "player";

        public const string Ticket = "ticket";
        public const string Ready = "ready";
        public const string JoinQueue = "join-queue";
        public const string LeaveQueue = "leave-queue";
        public const string Allowance = "allowance";

        public const string Deposit = "deposit";
        public const string DepositAdd = "deposit/add";
        public const string DepositWithdrawTicket = "deposit/withdraw-ticket";
        public const string TransactionHistory = "transaction-history";
    }
}
