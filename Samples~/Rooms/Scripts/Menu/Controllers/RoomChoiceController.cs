using System.Collections.Generic;
using UnityEngine;
using System;
using Elympics;
using JetBrains.Annotations;
using Elympics.Rooms.Models;

public class RoomChoiceController : BaseWindow
{
    [SerializeField] private RectTransform roomListContentParent;
    [SerializeField] private RoomRecordController roomRecordPrefab;
    [SerializeField] private JoinWithCodePopupController joinRoomWithCodePopup;
    [SerializeField] private BasePopup errorPopup;

    private Dictionary<Guid, RoomRecordController> existingRooms;

    public Action<int> ListLengthChanged;

    private void Start()
    {
        joinRoomWithCodePopup.Init(TryJoinRoomByCode);

        InitializeRoomList();
    }

    private async void InitializeRoomList()
    {
        existingRooms = new();

        try
        {
            Debug.Log("Fetched existing room list");
            /*
            var fetchedRooms = await RoomsManager.ListPublicRooms(0);
            */
            //TODO: Adjust output type 
            foreach (var fetchedRoom in TemporaryTestRoomList.Rooms)
            {
                AddRoomRecord(fetchedRoom.Value);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Room list not initialized: {e.Message}");
        }
    }

    private void AddRoomRecord(RoomStateChanged room)
    {
        RoomRecordController newRoomRecord = Instantiate(roomRecordPrefab, roomListContentParent);
        newRoomRecord.Init(room, TryJoinRoomById, joinRoomWithCodePopup.SetAndShow);
        existingRooms.Add(room.RoomId, newRoomRecord);
        ListLengthChanged?.Invoke(existingRooms.Count);
    }

    private void RemoveRoomRecord(Guid roomId)
    {
        Destroy(existingRooms[roomId].gameObject);
        existingRooms.Remove(roomId);
        ListLengthChanged?.Invoke(existingRooms.Count);
    }

    private void TryJoinRoomById(Guid roomId) => TryJoinRoom(roomId, null, errorPopup);
    private void TryJoinRoomByCode(string joinCode) => TryJoinRoom(null, joinCode, joinRoomWithCodePopup.PopupView);

    private async void TryJoinRoom(Guid? roomId, string joinCode, BasePopup errorPopup)
    {
        try
        {
            await RoomsUtility.RoomsManager.JoinRoom(roomId, joinCode, null);
        }
        catch (Exception e)
        {
            string errorMessage = $"Joining failed: {e.Message}";
            Debug.LogError(errorMessage);
            errorPopup.SetMessage(errorMessage);
            errorPopup.Show();
        }
    }

    public override void Show()
    {
        base.Show();

        joinRoomWithCodePopup.Hide();
    }

    [UsedImplicitly]
    public void ShowTitleScreen() => RoomsNavigationController.Instance.ShowTitleScreen();

    [UsedImplicitly]
    public void ShowRoomCreationView() => RoomsNavigationController.Instance.ShowRoomCreationView();
}

public static class TemporaryTestRoomList
{
    public static readonly Dictionary<Guid, RoomStateChanged> Rooms = new();

    static TemporaryTestRoomList()
    {
        var roomId1 = Guid.Parse("383dc2ee-e1bf-4224-aaeb-66e425da8702");
        var roomId3 = Guid.Parse("02c0fe92-ce91-475e-90bf-12c8cea23016");
        var roomId4 = Guid.Parse("04c0fe92-ce91-475e-90bf-12c8cea23016");

        Rooms.Add(roomId1, new RoomStateChanged(
            roomId1,
            DateTime.Now,
            new List<UserInfo> { new(Guid.NewGuid(), 0, false) },
            true,
            new MatchmakingData(
                MatchmakingState.Unlocked,
                DateTime.Now,
                "q1",
                1,
                2,
                null,
                new Dictionary<string, string> { { ":pub:SampleData", "public data" }, { "PrivateData", "private data" } }
            ),
            false,
            false,
            "Pair1v1Public",
            "CCCCCCCC",
            new Dictionary<string, string>()
        ));


        Rooms.Add(roomId3, new RoomStateChanged(
            roomId3,
            DateTime.Now,
            new List<UserInfo> { new(Guid.NewGuid(), 0, true) },
            true,
            new MatchmakingData(
                MatchmakingState.Unlocked,
                DateTime.Now,
                "q1",
                1,
                2,
                null,
                new Dictionary<string, string> { { ":pub:SampleData", "public data" }, { "PrivateData", "private data" } }
            ),
            true,
            false,
            "1v1Private",
            "AAAAAAAA",
            new Dictionary<string, string>()
        ));

        Rooms.Add(roomId4, new RoomStateChanged(
            roomId4,
            DateTime.Now,
            new List<UserInfo> { new(Guid.NewGuid(), 0, true), new(Guid.NewGuid(), 0, true) },
            true,
            new MatchmakingData(
                MatchmakingState.Unlocked,
                DateTime.Now,
                "q1",
                1,
                2,
                null,
                new Dictionary<string, string> { { ":pub:SampleData", "public data" }, { "PrivateData", "private data" } }
            ),
            false,
            false,
            "FullRoom",
            "WWWWWWWW",
            new Dictionary<string, string>()
        ));
    }
}