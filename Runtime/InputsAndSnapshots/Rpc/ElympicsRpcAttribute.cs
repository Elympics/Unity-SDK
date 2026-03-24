using System;

namespace Elympics
{
    public enum ElympicsRpcDirection
    {
        /// RPC is sent from a player instance to the server instance and executed there.
        PlayerToServer,
        /// RPC is sent from the server instance to all player instances and executed there.
        ServerToPlayers,
    }

    public struct ElympicsRpcProperties
    {
        public ElympicsRpcDirection Direction { get; set; }
        public bool WaitForTick { get; set; }

        public ElympicsRpcProperties(ElympicsRpcDirection direction) =>
            (Direction, WaitForTick) = (direction, true);
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class ElympicsRpcAttribute : Attribute
    {
        /// <summary>
        /// After being received, should the RPC wait for the tick it was sent on?
        /// If false, it will execute as soon as it arrives (but still wait for the correct part of the current tick loop).
        /// </summary>
        /// <value>true (by default)</value>
        public bool WaitForTick
        {
            get => Properties.WaitForTick;
            set => Properties.WaitForTick = value;
        }

        internal ElympicsRpcProperties Properties;

        /// <summary>
        /// Marks the method of <see cref="ElympicsMonoBehaviour"/> as a Remote Procedure Call.
        /// </summary>
        /// <param name="direction">Who is the sender and who is the addressee of the call?</param>
        public ElympicsRpcAttribute(ElympicsRpcDirection direction) =>
            Properties = new ElympicsRpcProperties(direction);
    }
}
