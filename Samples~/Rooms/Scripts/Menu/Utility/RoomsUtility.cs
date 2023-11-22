using Elympics;

public static class RoomsUtility
{
    public const string SampleDataKey = ":pub:SampleData";

    public static IRoomsManager RoomsManager => ElympicsLobbyClient.Instance.RoomsManager;

    public static int RoomCapacity(IRoom room) => (int)(room.State.MatchmakingData.TeamCount * room.State.MatchmakingData.TeamSize);
}
