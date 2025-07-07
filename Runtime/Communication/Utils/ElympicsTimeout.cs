using System;
namespace Elympics.Communication.Utils
{
    internal static class ElympicsTimeout
    {
        public static TimeSpan AuthenticationTimeout = TimeSpan.FromSeconds(30);
        public static TimeSpan RoomStateChangeConfirmationTimeout = TimeSpan.FromSeconds(15);
        public static TimeSpan ForceMatchmakingCancellationTimeout = TimeSpan.FromSeconds(10);
        public static TimeSpan WebSocketOpeningTimeout = TimeSpan.FromSeconds(10);
        public static TimeSpan WebSocketOperationTimeout = TimeSpan.FromSeconds(10);
        public static TimeSpan WebSocketHeartbeatTimeout = TimeSpan.FromSeconds(30);
    }
}
