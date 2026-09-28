#include "ElympicsHost.h"

#include <atomic>
#include <deque>
#include <mutex>
#include <string>
#include <utility>

// Must match ElympicsNativeHostBridge.ApiVersion and the command kinds in C#.
static const int ApiVersion = 1;

enum CommandKind
{
    CommandNone = 0,
    CommandLaunchMatch = 1,
    CommandAbortMatch = 2,
    CommandReturnToBoot = 3,
    CommandQuitApplication = 4,
    CommandPing = 5,
};

static std::atomic<ElympicsHostEventHandler> eventHandler{nullptr};
static std::mutex commandsMutex;
static std::deque<std::pair<int, std::string>> commands;
// The last polled argument, kept alive until the next poll so C# can copy it.
static std::string polledArgument;

static void Enqueue(int kind, const char *argument)
{
    std::lock_guard<std::mutex> lock(commandsMutex);
    commands.emplace_back(kind, argument != nullptr ? argument : "");
}

extern "C" {

ELYMPICS_HOST_EXPORT void ElympicsHost_RegisterEventHandler(ElympicsHostEventHandler handler)
{
    eventHandler.store(handler);
}

ELYMPICS_HOST_EXPORT void ElympicsHost_LaunchMatch(const char *requestJson) { Enqueue(CommandLaunchMatch, requestJson); }
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

// Called by C# on the main thread only. Returns the next command's argument (valid until the
// next call) and its kind, or NULL with kind CommandNone if the queue is empty.
ELYMPICS_HOST_EXPORT const char *ElympicsHost_PollCommand(int *kind)
{
    std::lock_guard<std::mutex> lock(commandsMutex);
    if (commands.empty())
    {
        if (kind != nullptr)
            *kind = CommandNone;
        return nullptr;
    }
    if (kind != nullptr)
        *kind = commands.front().first;
    polledArgument = std::move(commands.front().second);
    commands.pop_front();
    return polledArgument.c_str();
}

ELYMPICS_HOST_EXPORT int ElympicsHost_GetApiVersion(void)
{
    return ApiVersion;
}

}
