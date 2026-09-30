#include "ElympicsHost.h"

#include <atomic>
#include <deque>
#include <mutex>
#include <string>

// Must match ElympicsNativeHostBridge.ApiVersion and the command kinds in C#.
static const int ApiVersion = 2;

enum CommandKind
{
    CommandNone = 0,
    // 1 was the generic launch of API version 1.
    CommandAbortMatch = 2,
    CommandReturnToBoot = 3,
    CommandQuitApplication = 4,
    CommandPing = 5,
    CommandLaunchOnline = 6,
    CommandLaunchSinglePlayer = 7,
    CommandLaunchHalfRemoteClient = 8,
    CommandLaunchHalfRemoteServer = 9,
    CommandLaunchSnapshotReplay = 10,
};

struct Command
{
    int kind;
    std::string first;
    std::string second;
    std::string third;
};

static std::atomic<ElympicsHostEventHandler> eventHandler{nullptr};
static std::mutex commandsMutex;
static std::deque<Command> commands;
// The last polled command, kept alive until the next poll so C# can copy its strings.
static Command polled;

static std::string Copy(const char *text) { return text != nullptr ? text : ""; }

static void Enqueue(int kind, const char *first, const char *second = nullptr, const char *third = nullptr)
{
    std::lock_guard<std::mutex> lock(commandsMutex);
    commands.push_back(Command{kind, Copy(first), Copy(second), Copy(third)});
}

extern "C" {

ELYMPICS_HOST_EXPORT void ElympicsHost_RegisterEventHandler(ElympicsHostEventHandler handler)
{
    eventHandler.store(handler);
}

ELYMPICS_HOST_EXPORT void ElympicsHost_LaunchOnline(const char *gameId, const char *dataJson, const char *optionsJson) { Enqueue(CommandLaunchOnline, gameId, dataJson, optionsJson); }
ELYMPICS_HOST_EXPORT void ElympicsHost_LaunchSinglePlayer(const char *gameId, const char *dataJson, const char *optionsJson) { Enqueue(CommandLaunchSinglePlayer, gameId, dataJson, optionsJson); }
ELYMPICS_HOST_EXPORT void ElympicsHost_LaunchHalfRemoteClient(const char *gameId, const char *dataJson, const char *optionsJson) { Enqueue(CommandLaunchHalfRemoteClient, gameId, dataJson, optionsJson); }
ELYMPICS_HOST_EXPORT void ElympicsHost_LaunchHalfRemoteServer(const char *gameId, const char *dataJson, const char *optionsJson) { Enqueue(CommandLaunchHalfRemoteServer, gameId, dataJson, optionsJson); }
ELYMPICS_HOST_EXPORT void ElympicsHost_LaunchSnapshotReplay(const char *gameId, const char *dataJson, const char *optionsJson) { Enqueue(CommandLaunchSnapshotReplay, gameId, dataJson, optionsJson); }
ELYMPICS_HOST_EXPORT void ElympicsHost_AbortMatch(const char *requestId) { Enqueue(CommandAbortMatch, requestId); }
ELYMPICS_HOST_EXPORT void ElympicsHost_ReturnToBoot(const char *requestId) { Enqueue(CommandReturnToBoot, requestId); }
ELYMPICS_HOST_EXPORT void ElympicsHost_QuitApplication(const char *reason) { Enqueue(CommandQuitApplication, reason); }
ELYMPICS_HOST_EXPORT void ElympicsHost_Ping(const char *requestId) { Enqueue(CommandPing, requestId); }

// Returns 1 if a handler received the event, 0 if none is registered (C# logs that).
ELYMPICS_HOST_EXPORT int ElympicsHost_DispatchEvent(const char *type, const char *requestId, const char *payloadJson)
{
    const ElympicsHostEventHandler handler = eventHandler.load();
    if (handler == nullptr)
        return 0;
    handler(type != nullptr ? type : "", requestId != nullptr ? requestId : "", payloadJson != nullptr ? payloadJson : "");
    return 1;
}

// Called by C# on the main thread only. Returns the next command's first string and sets its kind and
// other two strings ("" if unused), all valid until the next call. Returns NULL with kind CommandNone
// (and NULL strings) if the queue is empty.
ELYMPICS_HOST_EXPORT const char *ElympicsHost_PollCommand(int *kind, const char **second, const char **third)
{
    std::lock_guard<std::mutex> lock(commandsMutex);
    if (commands.empty())
    {
        if (kind != nullptr)
            *kind = CommandNone;
        if (second != nullptr)
            *second = nullptr;
        if (third != nullptr)
            *third = nullptr;
        return nullptr;
    }
    polled = std::move(commands.front());
    commands.pop_front();
    if (kind != nullptr)
        *kind = polled.kind;
    if (second != nullptr)
        *second = polled.second.c_str();
    if (third != nullptr)
        *third = polled.third.c_str();
    return polled.first.c_str();
}

ELYMPICS_HOST_EXPORT int ElympicsHost_GetApiVersion(void)
{
    return ApiVersion;
}

}
