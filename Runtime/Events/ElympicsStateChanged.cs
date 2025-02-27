#nullable enable

namespace Elympics.AssemblyCommunicator.Events
{
    public struct ElympicsStateChanged
    {
        public ElympicsState PreviousState;
        public ElympicsState NewState;
    }
}
