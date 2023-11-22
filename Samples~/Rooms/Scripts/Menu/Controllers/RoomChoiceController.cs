using System;
using System.Collections.Generic;
using Elympics;
using JetBrains.Annotations;
using UnityEngine;

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

    private void InitializeRoomList()
    {
        existingRooms = new();

        try
        {
            foreach (var room in RoomsUtility.RoomsManager.ListAvailableRooms())
            {
                AddRoomRecord(room);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Room list not initialized: {e.Message}");
        }

        RoomsUtility.RoomsManager.RoomListUpdated += OnRoomsListUpdated;
    }

    private void OnDestroy()
    {
        RoomsUtility.RoomsManager.RoomListUpdated -= OnRoomsListUpdated;
    }

    private void OnRoomsListUpdated(RoomListUpdatedArgs obj)
    {
        foreach (var updatedRoomId in obj.RoomIds)
        {
            existingRooms[updatedRoomId].Reset();
        }
    }

    private void AddRoomRecord(IRoom room)
    {
        var newRoomRecord = Instantiate(roomRecordPrefab, roomListContentParent);
        newRoomRecord.Init(room, TryJoinRoomById, joinRoomWithCodePopup.SetAndShow);
        existingRooms.Add(room.RoomId, newRoomRecord);
        ListLengthChanged?.Invoke(existingRooms.Count);
    }

    private void RemoveRoomRecord(Guid roomId)
    {
        Destroy(existingRooms[roomId].gameObject);
        _ = existingRooms.Remove(roomId);
        ListLengthChanged?.Invoke(existingRooms.Count);
    }

    private void TryJoinRoomById(Guid roomId) => TryJoinRoom(roomId, null, errorPopup);
    private void TryJoinRoomByCode(string joinCode) => TryJoinRoom(null, joinCode, joinRoomWithCodePopup.PopupView);

    private async void TryJoinRoom(Guid? roomId, string joinCode, BasePopup errorPopup)
    {
        try
        {
            _ = await RoomsUtility.RoomsManager.JoinRoom(roomId, joinCode, null);
        }
        catch (Exception e)
        {
            var errorMessage = $"Joining failed: {e.Message}";
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
