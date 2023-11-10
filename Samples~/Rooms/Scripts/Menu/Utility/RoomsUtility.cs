using Elympics;

public static class RoomsUtility
{
    public const int MaxPlayers = 2;
    public const string SampleDataKey = ":pub:SampleData";

    public static IRoomsManager RoomsManager => ElympicsLobbyClient.Instance.RoomsManager;
}
