using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System;
using Elympics;
using JetBrains.Annotations;

public class RoomChoiceController : BaseWindow
{
    [SerializeField] private RectTransform roomListContentParent;
    [SerializeField] private RoomRecordController roomRecordPrefab;
    [SerializeField] private JoinWithCodePopupController joinRoomWithCodePopup;
    [SerializeField] private RoomCreationController roomCreationController;

    private Dictionary<Guid, RoomRecordController> existingRooms;

    private IRoomsManager RoomsManager => ElympicsLobbyClient.Instance.RoomsManager;

    private void Start()
    {
        Assert.IsNotNull(roomCreationController);

        joinRoomWithCodePopup.Init(TryJoinRoom);

        InitializeRoomList();
    }

    private async void InitializeRoomList()
    {
        existingRooms = new();

        try
        {
            var fetchedRooms = await RoomsManager.ListPublicRooms(0);

            //TODO: Adjust output type 
            foreach (var fetchedRoom in fetchedRooms)
            {
                //AddRoomRecord(fetchedRoom);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Room list not initialized: {e.Message}");
        }
    }

    private void AddRoomRecord(IRoom room)
    {
        RoomRecordController newRoomRecord = Instantiate(roomRecordPrefab, roomListContentParent);
        newRoomRecord.Init(room, TryJoinRoom, joinRoomWithCodePopup.SetAndShow);
        existingRooms.Add(room.RoomId, newRoomRecord);
    }

    private void RemoveRoomRecord(Guid roomId)
    {
        Destroy(existingRooms[roomId].gameObject);
        existingRooms.Remove(roomId);
    }

    private void TryJoinRoom(IRoom room) => TryJoinRoom(room, null);

    private async void TryJoinRoom(IRoom room, BasePopup errorPopup)
    {
        try
        {
            await RoomsManager.JoinRoom(room.RoomId, room.JoinCode, null);
        }
        catch (Exception e)
        {
            string errorMessage = $"Joining failed: {e.Message}";
            Debug.LogError(errorMessage);
            errorPopup.LogText(errorMessage);
        }
    }

    [UsedImplicitly]
    public void ShowRoomCreationView() => roomCreationController.Show();
}
