using MessagePack;

namespace Elympics
{
    /// <summary>Sent by a player to confirm a successful launch. See <see cref="IElympics.MarkReady"/>.</summary>
    [MessagePackObject]
    public class ElympicsReadyMessage : IToServer
    { }
}
