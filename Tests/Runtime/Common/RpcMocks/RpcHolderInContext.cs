namespace Elympics.Tests.RpcMocks
{
    public abstract class RpcHolderInContext : RpcHolder
    {
        public bool ShouldCallPlayerToServerMethod { get; set; }
        public bool ShouldCallServerToPlayerMethod { get; set; }

        public override void Reset()
        {
            base.Reset();
            ShouldCallPlayerToServerMethod = false;
            ShouldCallServerToPlayerMethod = false;
        }

        public abstract void Setup(ElympicsBaseTest elympicsInstance);
        public abstract void Act(ElympicsBaseTest elympicsInstance);

        protected void CallRpc()
        {
            if (ShouldCallPlayerToServerMethod)
                PlayerToServerMethod();
            if (ShouldCallServerToPlayerMethod)
                ServerToPlayersMethod();
        }
    }
}

