#nullable enable

namespace Elympics.Core.Logger.State
{
    internal sealed class LobbyState : IVisitableState
    {
        public readonly string Region;

        public LobbyState(string region) => Region = region;

        public bool Visit(IStateVisitor visitor)
        {
            visitor.ProcessProperty(nameof(Region), Region);
            return true;
        }
    }
}
