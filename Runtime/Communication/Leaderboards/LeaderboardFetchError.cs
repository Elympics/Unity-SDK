namespace Elympics
{
    public enum LeaderboardFetchError
    {
        UnknownError = 0,

        NoRecords = 101,
        PageLessThanOne = 102,
        PageGreaterThanMax = 103,
        NoScoresForUser = 104,

        RequestAlreadyInProgress = 201,

        NotAuthenticated = 301,
    }
}
