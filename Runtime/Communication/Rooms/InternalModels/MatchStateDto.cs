namespace Elympics.Communication.Rooms.Models
{
    public enum MatchStateDto
    {
        Initializing = 1,
        Running = 2,
        RunningEnded = 3,
        InitializingFailed = 4,
        RunningFailed = 5,
        ProcessCrashed = 6,
        MatchmakingFailed = 7,
    }
}
