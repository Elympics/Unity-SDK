using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Elympics;
using JetBrains.Annotations;
using UnityEngine;

public class RoomChoiceController : BaseWindow
{
    [SerializeField] private RectTransform roomListContentParent;
    [SerializeField] private RoomRecordController roomRecordPrefab;
    [SerializeField] private JoinWithCodePopupController joinRoomWithCodePopup;
    [SerializeField] private BasePopup errorPopup;

    private Dictionary<Guid, RoomRecordController> _roomRecordsLookup;

    public Action<int> ListLengthChanged;

    private void Start()
    {
        joinRoomWithCodePopup.Init(TryJoinRoomByCode);

        InitializeRoomList();
    }

    private void InitializeRoomList()
    {
        _roomRecordsLookup = new();

        foreach (var room in RoomsUtility.RoomsManager.ListAvailableRooms())
        {
            AddRoomRecord(room);
        }

        RoomsUtility.RoomsManager.RoomListUpdated += OnRoomsListUpdated;
    }

    private void OnDestroy()
    {
        RoomsUtility.RoomsManager.RoomListUpdated -= OnRoomsListUpdated;
    }

    private void OnRoomsListUpdated(RoomListUpdatedArgs obj)
    {
        var roomList = RoomsUtility.RoomsManager.ListAvailableRooms();

        foreach (var updatedRoomId in obj.RoomIds)
        {
            var updatedRoom = roomList.FirstOrDefault(x => x.RoomId == updatedRoomId);

            if (updatedRoom == null)
                RemoveRoomRecord(updatedRoomId);
            else if (_roomRecordsLookup.TryGetValue(updatedRoomId, out var recordController))
                recordController.Reset();
            else
                AddRoomRecord(updatedRoom);
        }
    }

    private void AddRoomRecord(IRoom room)
    {
        var newRoomRecord = Instantiate(roomRecordPrefab, roomListContentParent);
        newRoomRecord.Init(room, TryJoinRoomById, joinRoomWithCodePopup.SetAndShow);
        _roomRecordsLookup.Add(room.RoomId, newRoomRecord);
        ListLengthChanged?.Invoke(_roomRecordsLookup.Count);
    }

    private void RemoveRoomRecord(Guid roomId)
    {
        Destroy(_roomRecordsLookup[roomId].gameObject);
        _ = _roomRecordsLookup.Remove(roomId);
        ListLengthChanged?.Invoke(_roomRecordsLookup.Count);
    }

    private void TryJoinRoomById(Guid roomId) => TryJoinRoom(roomId, null, errorPopup);
    private void TryJoinRoomByCode(string joinCode, Guid? roomId = null) => TryJoinRoom(roomId, joinCode, joinRoomWithCodePopup.PopupView);

    private async void TryJoinRoom(Guid? roomId, string joinCode, BasePopup errorPopup)
    {
        try
        {
            _ = await RoomsUtility.RoomsManager.JoinRoom(roomId, joinCode, null);
        }
        catch (Exception e)
        {
            await UniTask.SwitchToMainThread();

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
