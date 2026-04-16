using System;
using JetBrains.Annotations;
using UnityEngine;

public class RoomCreationController : BaseWindow
{
    [SerializeField] private RoomCreationView roomViewElements;
    [SerializeField] private AdditionalRoomData additionalRoomData;
    private RoomController _roomController;
    private Action<Guid> onRoomCreated;
    private string _queue;
    public void Init(RoomController roomController)
    {
        _roomController = roomController;
        roomViewElements.Init(additionalRoomData.GetDataHolderUi());
        onRoomCreated += _roomController.InitRoom;
    }
    public void Deinit() => onRoomCreated -= _roomController.InitRoom;
    public override void Show()
    {
        base.Show();
        roomViewElements.ManageWindowInteractability(true);
        roomViewElements.RoomName.text = GenerateRandomRoomName();
        roomViewElements.RoomPrivacy.Restart();
        additionalRoomData.Load();
    }
    public override void Hide()
    {
        additionalRoomData.ClearData();
        base.Hide();
    }
    private string GenerateRandomRoomName() => $"Room {UnityEngine.Random.Range(0, 10000)}";

    [UsedImplicitly]
    public void ShowRoomChoiceView() => RoomsNavigationController.Instance.ShowRoomChoiceView();
    [UsedImplicitly]
    public void SetQueueAndCreateRoom()
    {
        SetQueue();
        CreateRoom();
    }
    private void SetQueue()
    {
        var selectedTeamValue = roomViewElements.GetSelectedGameMode();
        var queueTypes = RoomsUtility.QueueTypes;
        int queueIndex;
        for (queueIndex = 0; queueIndex < queueTypes.Length; queueIndex++)
        {
            if (selectedTeamValue == queueTypes[queueIndex])
                break;
        }
        _queue = queueTypes[queueIndex];
    }
    private async void CreateRoom()
    {
        try
        {
            roomViewElements.ManageWindowInteractability(false);
            var isSingleTeam = _queue == "solo";
            var matchmakingData = additionalRoomData.GetMatchmakingRoomData();
            var roomData = additionalRoomData.GetCustomRoomData();
            if (!matchmakingData.ContainsKey(RoomsUtility.SampleMatchmakingDataKey))
                matchmakingData.Add(RoomsUtility.SampleMatchmakingDataKey, RoomsUtility.SampleMatchmakingDataValue);
            var room = await RoomsUtility.RoomsManager.CreateAndJoinRoom(roomViewElements.RoomName.text, _queue, isSingleTeam, roomViewElements.IsPrivate, matchmakingData, roomData);
            onRoomCreated?.Invoke(room.RoomId);

        }
        catch (Exception e)
        {
            roomViewElements.ManageWindowInteractability(true);
            Debug.LogError($"Room creation failed: {e.Message}");
        }
    }
}
