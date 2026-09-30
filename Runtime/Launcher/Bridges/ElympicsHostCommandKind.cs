namespace Elympics
{
    /// <summary>Host command kinds, shared by the native plugin queue (ElympicsHost.cpp) and the WebGL command callback (ElympicsHostBridge.jslib).</summary>
    internal enum ElympicsHostCommandKind
    {
        None = 0,
        // 1 was the generic launch of native API version 1.
        AbortMatch = 2,
        ReturnToBoot = 3,
        QuitApplication = 4,
        Ping = 5,
        LaunchOnline = 6,
        LaunchSinglePlayer = 7,
        LaunchHalfRemoteClient = 8,
        LaunchHalfRemoteServer = 9,
        LaunchSnapshotReplay = 10,
    }
}
