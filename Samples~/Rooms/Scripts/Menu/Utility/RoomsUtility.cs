using Elympics;

public static class RoomsUtility
{
    public const int MaxPlayers = 2;
    public const string SampleDataKey = "SampleData";

    public static IRoomsManager RoomsManager => ElympicsLobbyClient.Instance.RoomsManager;
}
