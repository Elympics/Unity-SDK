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
    }
}

