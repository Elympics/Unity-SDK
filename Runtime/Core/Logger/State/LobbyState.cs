#nullable enable

namespace Elympics.Core.Logger.State
{
    internal sealed class LobbyState : IVisitableState
    {
        public string Region;

        public LobbyState(string region) => Region = region;

        public void Visit(IStateVisitor visitor)
        {
            visitor.ProcessProperty(nameof(Region), Region);
        }
    }
}
