namespace Elympics
{
    public enum ErrorKind
    {
        Unspecified = 0,
        GameDoesNotExist = 1,
        GameVersionDoesNotExist = 2,
        QueueDoesNotExist = 3,
        RegionDoesNotExist = 4,
        RoomDoesNotExist = 5,
        RoomLocked = 6,
        RoomFull = 7,
        TeamFull = 8,
        Outdated = 9,
        AlreadyInRoom = 10,
        NotInRoom = 11,
        RoomAlreadyExists = 12,
        RoomWithoutMatchmaking = 13,
        AlreadyReady = 14,
        AlreadyNotReady = 15,
        NotHost = 16,
        NotEveryoneReady = 17,
        AlreadyInTeam = 18,
        InvalidTeam = 19,
        FailedToUnlockAfterSuccessfulRemove = 20,
        FailedToCancelMatchmakingWithTimeout = 21,
        RoomPrivate = 22,

        Throttle = 10000,
        InvalidMessage = 10001,
    }
}
