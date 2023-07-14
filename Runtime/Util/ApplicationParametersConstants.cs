namespace Elympics
{
    public partial class ApplicationParameters
    {
        public static class Factory
        {
            private const string ElympicsEnvironmentVariable = "ELYMPICS";
            private const string ElympicsBotEnvironmentVariable = "ELYMPICS_BOT";

            public static bool ShouldLoadElympicsOnlineServer() => IsElympicsEnvironmentVariableDefined() && !IsElympicsBotEnvironmentVariableDefined();
            public static bool ShouldLoadElympicsOnlineBot() => IsElympicsEnvironmentVariableDefined() && IsElympicsBotEnvironmentVariableDefined();
            public static bool ShouldLoadFromLobbyClient() => IsElympicsLobbyInitialized();
            public static bool ShouldLoadHalfRemoteServer() => IsUnityServer() && !IsUnityEditor();
            public static bool ShouldLoadHalfRemoteClient() => !IsUnityEditor();

            private static bool IsElympicsLobbyInitialized() => ElympicsLobbyClient.Instance != null;
            private static bool IsElympicsBotEnvironmentVariableDefined() => IsEnvironmentVariableDefined(ElympicsBotEnvironmentVariable);
            private static bool IsElympicsEnvironmentVariableDefined() => IsEnvironmentVariableDefined(ElympicsEnvironmentVariable);
        }

        public static class HalfRemote
        {
            internal const string PlayerIndexEnvironmentVariable = "ELYMPICS_HALF_REMOTE_PLAYER_INDEX";
            private const int PlayerIndexArgsIndex = 1;
            private const string PlayerIndexQueryParameter = "playerIndex";

            private const string IpEnvironmentVariable = "ELYMPICS_HALF_REMOTE_IP";
            private const int IpArgsIndex = 2;
            private const string IpQueryParameter = "ip";

            private const string PortEnvironmentVariable = "ELYMPICS_HALF_REMOTE_PORT";
            private const int PortArgsIndex = 3;
            private const string PortQueryParameter = "port";

            public static int GetPlayerIndex(ElympicsGameConfig config) => GetParameter(config.PlayerIndexForHalfRemoteMode, PlayerIndexEnvironmentVariable, PlayerIndexArgsIndex, PlayerIndexQueryParameter);
            public static string GetIp(ElympicsGameConfig config) => GetParameter(config.IpForHalfRemoteMode, IpEnvironmentVariable, IpArgsIndex, IpQueryParameter);
            public static int GetPort(ElympicsGameConfig config) => GetParameter(GetPortBasedOnWebUsage(config), PortEnvironmentVariable, PortArgsIndex, PortQueryParameter);

            private static int GetPortBasedOnWebUsage(ElympicsGameConfig config)
            {
                return config.UseWebInHalfRemote
                    ? config.WebPortForHalfRemoteMode
                    : config.TcpPortForHalfRemoteMode;
            }
        }
    }
}
