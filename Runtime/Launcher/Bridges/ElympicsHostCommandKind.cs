namespace Elympics
{
    /// <summary>Host command kinds, shared by the native plugin queue (ElympicsHost.cpp) and the WebGL command callback (ElympicsHostBridge.jslib).</summary>
    internal enum ElympicsHostCommandKind
    {
        None = 0,
        LaunchMatch = 1,
        AbortMatch = 2,
        ReturnToBoot = 3,
        QuitApplication = 4,
        Ping = 5,
    }
}
