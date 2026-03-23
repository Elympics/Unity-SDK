using System;

namespace Elympics
{
    public enum ElympicsRpcDirection
    {
        PlayerToServer,
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
        public bool WaitForTick
        {
            get => Properties.WaitForTick;
            set => Properties.WaitForTick = value;
        }

        internal ElympicsRpcProperties Properties;

        public ElympicsRpcAttribute(ElympicsRpcDirection direction) =>
            Properties = new ElympicsRpcProperties(direction);
    }
}
