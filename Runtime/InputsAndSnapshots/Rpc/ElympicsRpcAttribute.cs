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
        public readonly ElympicsRpcDirection Direction;

        public ElympicsRpcProperties(ElympicsRpcDirection direction) => Direction = direction;
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class ElympicsRpcAttribute : Attribute
    {
        internal readonly ElympicsRpcProperties Properties;

        public ElympicsRpcAttribute(ElympicsRpcDirection direction) => Properties = new ElympicsRpcProperties(direction);
    }
}
