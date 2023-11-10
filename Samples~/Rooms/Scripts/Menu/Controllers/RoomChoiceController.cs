using System.Collections.Generic;
using UnityEngine;
using System;
using Elympics;
using JetBrains.Annotations;
using Cysharp.Threading.Tasks;
using System.Threading;

public class RoomChoiceController : BaseWindow
{
    [SerializeField] private RectTransform roomListContentParent;
    [SerializeField] private RoomRecordController roomRecordPrefab;
    [SerializeField] private JoinWithCodePopupController joinRoomWithCodePopup;

    private Dictionary<Guid, RoomRecordController> existingRooms;

    private void Start()
    {
        joinRoomWithCodePopup.Init(TryJoinRoom);

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

            //TODO: Adjust output type 
            foreach (var fetchedRoom in fetchedRooms)
            {
                //AddRoomRecord(fetchedRoom);
            }
            */
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
            await RoomsUtility.RoomsManager.JoinRoom(room.RoomId, room.State.JoinCode, null);
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
