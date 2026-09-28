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

// requestJson: an ElympicsLaunchRequest serialized to JSON. Its "requestId" is echoed in the replies.
ELYMPICS_HOST_EXPORT void ElympicsHost_LaunchMatch(const char *requestJson);
// Leaves the current match whatever its state. Always succeeds; replies "abortAccepted" then "idle".
ELYMPICS_HOST_EXPORT void ElympicsHost_AbortMatch(const char *requestId);
// Same as ElympicsHost_AbortMatch, for readability once the match has ended.
ELYMPICS_HOST_EXPORT void ElympicsHost_ReturnToBoot(const char *requestId);
// Backup escape hatch: exits the application after a short best-effort teardown. Replies "quitting".
ELYMPICS_HOST_EXPORT void ElympicsHost_QuitApplication(const char *reason);
// Replies "pong".
ELYMPICS_HOST_EXPORT void ElympicsHost_Ping(const char *requestId);

// Used by the SDK's C# side only.
ELYMPICS_HOST_EXPORT int ElympicsHost_DispatchEvent(const char *type, const char *requestId, const char *payloadJson);
ELYMPICS_HOST_EXPORT const char *ElympicsHost_PollCommand(int *kind);
ELYMPICS_HOST_EXPORT int ElympicsHost_GetApiVersion(void);

#ifdef __cplusplus
}
#endif

#endif /* ElympicsHost_h */
