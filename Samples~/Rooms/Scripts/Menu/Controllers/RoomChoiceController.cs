using System.Collections.Generic;
using UnityEngine;
using System;
using Elympics;
using JetBrains.Annotations;

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
    }

    private void AddRoomRecord(IRoom room)
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
