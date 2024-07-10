using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elympics;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

public class RoomChoiceController : BaseWindow
{
    [SerializeField] private RectTransform roomListContentParent;
    [SerializeField] private RoomRecordController roomRecordPrefab;
    [SerializeField] private JoinWithCodePopupController joinRoomWithCodePopup;
    [SerializeField] private BasePopup errorPopup;
    [SerializeField] private Button joinByButton;
    [SerializeField] private Button quickgameButton;
    [SerializeField] private Button createButton;
    [SerializeField] private Button backButton;
    private RoomController _roomController;


    private Dictionary<Guid, RoomRecordController> _roomRecordsLookup = new();

    public Action<int> ListLengthChanged;
    public Action<Guid> OnRoomJoined;

    public void Init(RoomController roomController)
    {
        _roomController = roomController;
        joinRoomWithCodePopup.Init(TryJoinRoomByCode);
        OnRoomJoined += _roomController.InitRoom;
        RoomsUtility.RoomsManager.RoomListUpdated += OnRoomsListUpdated;
    }
    public void Deinit()
    {
        joinRoomWithCodePopup.Deinit();
        OnRoomJoined -= _roomController.InitRoom;
        RoomsUtility.RoomsManager.RoomListUpdated -= OnRoomsListUpdated;
        _roomController = null;
    }
    private void InitializeRoomList()
    {
        _roomRecordsLookup = new();

        foreach (var room in RoomsUtility.RoomsManager.ListAvailableRooms())
        {
            AddRoomRecord(room);
        }
    }
    public void ReinitializeRoomRecords()
    {
        foreach (var room in _roomRecordsLookup)
        {
            Destroy(room.Value.gameObject);
        }
        InitializeRoomList();
    }
    private void OnRoomsListUpdated(RoomListUpdatedArgs obj)
    {
        foreach (var updatedRoomId in obj.RoomIds)
        {
            if (!RoomsUtility.RoomsManager.TryGetAvailableRoom(updatedRoomId, out var updatedRoom))
                RemoveRoomRecord(updatedRoomId);
            else if (_roomRecordsLookup.TryGetValue(updatedRoomId, out var recordController))
                recordController.Reset();
            else
                AddRoomRecord(updatedRoom);

            if (RoomsUtility.RoomsManager.TryGetJoinedRoom(updatedRoomId, out var roomAviable))
                AddRoomRecord(roomAviable);
        }
    }

    private void AddRoomRecord(IRoom room)
    {
        var newRoomRecord = Instantiate(roomRecordPrefab, roomListContentParent);
        newRoomRecord.Init(room, TryJoinRoomById, joinRoomWithCodePopup.SetAndShow);
        foreach (var userInfo in room.State.Users)
        {
            if (userInfo.UserId == ElympicsLobbyClient.Instance.UserGuid)
                newRoomRecord.SetBackgroundColor(Color.green);
        }
        _roomRecordsLookup.Add(room.RoomId, newRoomRecord);
        ListLengthChanged?.Invoke(_roomRecordsLookup.Count);
    }

    private void RemoveRoomRecord(Guid roomId)
    {
        if (_roomRecordsLookup.TryGetValue(roomId, out var recordController))
        {
            _ = _roomRecordsLookup.Remove(roomId);
            Destroy(recordController.gameObject);
            ListLengthChanged?.Invoke(_roomRecordsLookup.Count);
        }
    }

    private void TryJoinRoomById(Guid roomId) => TryJoinRoom(roomId, null, errorPopup).Forget();
    private void TryJoinRoomByCode(string joinCode, Guid? roomId = null) => TryJoinRoom(roomId, joinCode, joinRoomWithCodePopup.PopupView).Forget();

    private async UniTaskVoid TryJoinRoom(Guid? roomId, string joinCode, BasePopup errorPopup)
    {
        try
        {
            ManageWindowInteractability(false);
            var room = await RoomsUtility.RoomsManager.JoinRoom(roomId, joinCode, null);
            OnRoomJoined?.Invoke(room.RoomId);
            ManageWindowInteractability(true);
        }
        catch (Exception e)
        {
            await UniTask.SwitchToMainThread();

            var errorMessage = $"Joining failed: {e.Message}";
            Debug.LogError(errorMessage);
            errorPopup.SetMessage(errorMessage);
            errorPopup.Show();
            ManageWindowInteractability(true);

        }
    }

    public override void Show()
    {
        base.Show();
        ManageWindowInteractability(true);
        joinRoomWithCodePopup.Hide();
    }

    [UsedImplicitly]
    public void ShowTitleScreen() => RoomsNavigationController.Instance.ShowTitleScreen();

    [UsedImplicitly]
    public void ShowRoomCreationView() => RoomsNavigationController.Instance.ShowRoomCreationView();
    [UsedImplicitly]
    public void StartQuickMatch() => TryStartQuickGame().Forget();
    private async UniTaskVoid TryStartQuickGame()
    {
        try
        {
            ManageWindowInteractability(false);
            var room = await ElympicsLobbyClient.Instance!.RoomsManager.StartQuickMatch("solo", null, null, new CancellationTokenSource()!.Token);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            ManageWindowInteractability(true);
        }
    }
    private void ManageWindowInteractability(bool interactable)
    {
        joinByButton.interactable = interactable;
        quickgameButton.interactable = interactable;
        createButton.interactable = interactable;
        backButton.interactable = interactable;
    }
}
