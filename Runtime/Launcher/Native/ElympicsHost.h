#ifndef ElympicsHost_h
#define ElympicsHost_h

// Native host API of the Elympics match launcher.
//
// A host that embeds Unity as a library (an iOS app, or a desktop process loading UnityPlayer)
// drives matches with the command functions below and receives the launcher's events through
// the handler it registers. See ElympicsHostProtocol in the SDK for event types and payloads.
//
// Threading: commands may be called from any thread, at any time — even before Unity has
// started. They are queued and executed on Unity's main thread in order. Events are delivered
// on Unity's main thread.

#ifdef __cplusplus
extern "C" {
#endif

// Exported so the host can link against these symbols, and `used` so the linker does not
// dead-strip them: nothing inside the Unity player calls the command functions, only the host.
#if defined(_WIN32)
#define ELYMPICS_HOST_EXPORT __declspec(dllexport)
#else
#define ELYMPICS_HOST_EXPORT __attribute__((visibility("default"), used))
#endif

// Host callback for Unity -> host events.
// type:        an event type, e.g. "ready", "launchSucceeded", "launchFailed", "idle".
// requestId:   the ID of the command that caused the event, or "" for unsolicited events.
// payloadJson: JSON of the event's payload, or "".
// The strings are owned by the caller: copy them synchronously, do not keep the pointers.
typedef void (*ElympicsHostEventHandler)(const char *type, const char *requestId, const char *payloadJson);

// Registers the event handler (NULL unregisters). Events emitted while no handler is
// registered are dropped, so register before the first command to receive "ready".
ELYMPICS_HOST_EXPORT void ElympicsHost_RegisterEventHandler(ElympicsHostEventHandler handler);

// Commands (host -> Unity). All strings are UTF-8 and copied; NULL is treated as "".

// Launch commands, one per match mode. Each takes:
// gameId:      the GUID of a game in ElympicsConfig's available games.
// dataJson:    the mode's launch data as flat JSON, or "" for the mode's defaults. The fields are those of the
//              C# *LaunchData types, camelCase; unset is "", [] or 0 (-1 for playerIndex). See Documentation~/MatchLauncher.md.
// optionsJson: {"requestId", "launchTimeoutSeconds", "postMatchTimeoutSeconds"}, or "" for defaults.
//              The requestId is echoed in the replies; timeouts of 0 mean the default, -1 never.
ELYMPICS_HOST_EXPORT void ElympicsHost_LaunchOnline(const char *gameId, const char *dataJson, const char *optionsJson);
ELYMPICS_HOST_EXPORT void ElympicsHost_LaunchSinglePlayer(const char *gameId, const char *dataJson, const char *optionsJson);
ELYMPICS_HOST_EXPORT void ElympicsHost_LaunchHalfRemoteClient(const char *gameId, const char *dataJson, const char *optionsJson);
// Not available on iOS, which cannot listen for connections: fails with "UnsupportedMode".
ELYMPICS_HOST_EXPORT void ElympicsHost_LaunchHalfRemoteServer(const char *gameId, const char *dataJson, const char *optionsJson);
ELYMPICS_HOST_EXPORT void ElympicsHost_LaunchSnapshotReplay(const char *gameId, const char *dataJson, const char *optionsJson);

// Ends the current match whatever its state and loads the boot scene. Always succeeds; replies "returnToBootAccepted" then "idle".
ELYMPICS_HOST_EXPORT void ElympicsHost_ReturnToBoot(const char *requestId);
// Backup escape hatch: exits the application after a short best-effort teardown. Replies "quitting".
ELYMPICS_HOST_EXPORT void ElympicsHost_QuitApplication(const char *reason);
// Replies "pong".
ELYMPICS_HOST_EXPORT void ElympicsHost_Ping(const char *requestId);

// Used by the SDK's C# side only.
ELYMPICS_HOST_EXPORT int ElympicsHost_DispatchEvent(const char *type, const char *requestId, const char *payloadJson);
ELYMPICS_HOST_EXPORT const char *ElympicsHost_PollCommand(int *kind, const char **second, const char **third);
ELYMPICS_HOST_EXPORT int ElympicsHost_GetApiVersion(void);

#ifdef __cplusplus
}
#endif

#endif /* ElympicsHost_h */
