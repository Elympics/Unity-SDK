using System;
using Elympics;

public static class RoomsUtility
{
    public const string SampleMatchmakingDataKey = ":pub:scs:bet";
    public const string SampleMatchmakingDataValue = "sampleMatchmakingDataValue";
    public const string SampleRoomDataKey = "sampleRoomDataKey";
    public const string SampleRoomDataValue = "sampleRoomDataValue";
    public static IRoomsManager RoomsManager => ElympicsLobbyClient.Instance.RoomsManager;
    public static Guid LastConnectedRoom { get; set; }
    public static int RoomCapacity(IRoom room) => (int)(room.State.MatchmakingData.TeamCount * room.State.MatchmakingData.TeamSize);

    public static readonly string[] QueueTypes = { "solo", "duel", "ffa", "2v2", "3v3", "4v4" };
}
