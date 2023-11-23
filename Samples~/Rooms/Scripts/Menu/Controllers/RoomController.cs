using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Elympics;
using Elympics.Rooms.Models;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomController : BaseWindow
{
    private const int MatchmakingCountdownSeconds = 3;

    [SerializeField] private CanvasGroup roomCanvasGroup;
    [SerializeField] private RoomViewBaseElements roomViewElements;
    [SerializeField] private TextMeshProUGUI joinCode;

    [SerializeField] private Button readyButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private PlayerSeat[] playerSeats;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private BasePopup leavePopup;

    private readonly Dictionary<Guid, PlayerSeat> _seatLookup = new();
    private IRoom _currentRoom;

    private static Guid MyUserId => ElympicsLobbyClient.Instance.AuthData.UserId;
    private bool AmIHost => MyUserId.Equals(_currentRoom.State.Host.UserId);
    private bool IsActive => roomCanvasGroup.blocksRaycasts;

    #region Events integration
    private void Awake()
    {
        if (ElympicsLobbyClient.Instance.IsAuthenticated)
            SubscribeToRoomEvents();
        else
            ElympicsLobbyClient.Instance.AuthenticationSucceeded += (_) => SubscribeToRoomEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromRoomEvents();
    }

    private void OnApplicationQuit()
    {
        if (_currentRoom != null && _currentRoom.State.MatchmakingData.MatchmakingState == MatchmakingState.Unlocked)
            _ = _currentRoom.Leave();
    }

    private void SubscribeToRoomEvents()
    {
        if (!ElympicsLobbyClient.Instance.IsAuthenticated)
            return;

        RoomsUtility.RoomsManager.JoinedRoom += SetUpRoomData;
        RoomsUtility.RoomsManager.LeftRoom += RoomLeaveFeedback;

        RoomsUtility.RoomsManager.UserJoined += AddPlayer;
        RoomsUtility.RoomsManager.UserLeft += RemovePlayer;
        RoomsUtility.RoomsManager.UserCountChanged += ManageRoomFill;
        RoomsUtility.RoomsManager.HostChanged += ManageHostIndicatorState;
        RoomsUtility.RoomsManager.UserReadinessChanged += ManageReadiness;

        RoomsUtility.RoomsManager.RoomNameChanged += ManageRoomName;
        RoomsUtility.RoomsManager.RoomPublicnessChanged += ManageRoomPrivacy;
        RoomsUtility.RoomsManager.CustomMatchmakingDataChanged += ManageAdditionalData;

        RoomsUtility.RoomsManager.MatchmakingEnded += OnMatchmakingEnded;
        RoomsUtility.RoomsManager.MatchmakingDataChanged += OnMatchmakingStateChanged;

        StartCoroutine(TrackListAfterWait(3));
    }

    // TODO: remove this temporary solution after adding to the SDK event for websocket connection
    private IEnumerator TrackListAfterWait(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        Debug.Log("Initialized room list tracking");
        _ = RoomsUtility.RoomsManager.StartTrackingAvailableRooms();
    }

    private void UnsubscribeFromRoomEvents()
    {
        if (!ElympicsLobbyClient.Instance.IsAuthenticated)
            return;

        RoomsUtility.RoomsManager.JoinedRoom -= SetUpRoomData;
        RoomsUtility.RoomsManager.LeftRoom -= RoomLeaveFeedback;

        RoomsUtility.RoomsManager.UserJoined -= AddPlayer;
        RoomsUtility.RoomsManager.UserLeft -= RemovePlayer;
        RoomsUtility.RoomsManager.UserCountChanged -= ManageRoomFill;
        RoomsUtility.RoomsManager.HostChanged -= ManageHostIndicatorState;
        RoomsUtility.RoomsManager.UserReadinessChanged -= ManageReadiness;

        RoomsUtility.RoomsManager.RoomNameChanged -= ManageRoomName;
        RoomsUtility.RoomsManager.RoomPublicnessChanged -= ManageRoomPrivacy;
        RoomsUtility.RoomsManager.CustomMatchmakingDataChanged -= ManageAdditionalData;

        RoomsUtility.RoomsManager.MatchmakingEnded -= OnMatchmakingEnded;
        RoomsUtility.RoomsManager.MatchmakingDataChanged -= OnMatchmakingStateChanged;

        try
        {
            //_ = RoomsUtility.RoomsManager.StopTrackingAvailableRooms();
        }
        catch { }
    }
    #endregion

    private void SetUpRoomData(JoinedRoomArgs obj)
    {
        if (!RoomsUtility.RoomsManager.TryGetJoinedRoom(obj.RoomId, out _currentRoom))
            Debug.LogError("Joined room not found!");

        RoomsNavigationController.Instance.ShowRoomView();

        if (_currentRoom.State.Users.Any(x => x.UserId == MyUserId && x.TeamIndex.HasValue))
            return;
        var firstUser = _currentRoom.State.Users.First();
        _ = _currentRoom.ChangeTeam(firstUser.TeamIndex.HasValue ? 1 - firstUser.TeamIndex : 0);
    }

    private void RoomLeaveFeedback(LeftRoomArgs obj)
    {
        Debug.Log("You've just left the room.");
    }

    private bool TryTakeSeat(PlayerSeat seat, Guid userId)
    {
        if (seat.IsOccupied)
            return false;

        seat.SetPlayer(userId, MyUserId.Equals(userId));
        _seatLookup[userId] = seat;
        return true;
    }

    private void AddPlayer(UserJoinedArgs obj)
    {
        foreach (var seat in playerSeats)
        {
            if (TryTakeSeat(seat, obj.User.UserId))
                break;
        }
    }
    private void RemovePlayer(UserLeftArgs obj)
    {
        _seatLookup[obj.User.UserId].SetEmpty();
        _ = _seatLookup.Remove(obj.User.UserId);
    }

    private void ManageRoomFill(UserCountChangedArgs obj) => ManageRoomFill((int)obj.UserCount);

    private void ManageRoomFill(int userCount)
    {
        var allSeatsFull = userCount == RoomsUtility.RoomCapacity(_currentRoom);

        readyButton.gameObject.SetActive(allSeatsFull);
        statusText.text = allSeatsFull ? RoomStatusMessages.WaitingForReadyMessage : RoomStatusMessages.WaitingForPlayerToJoinMessage;
    }

    private void ManageHostIndicatorState(HostChangedArgs obj)
    {
        var oldSeat = _seatLookup[obj.UserId];

        if (TryTakeSeat(playerSeats.First(), obj.UserId))
        {
            oldSeat.SetEmpty();

            if (MyUserId.Equals(obj.UserId))
            {
                roomViewElements.ManageInteractability(true);
            }
        }

        _seatLookup[obj.UserId].SetHostIndicator();
    }

    private void ManageReadiness(UserReadinessChangedArgs obj)
    {
        if (!obj.IsReady)
            LockPlayersInRoom(false);

        if (MyUserId.Equals(obj.UserId))
        {
            readyButton.interactable = !obj.IsReady;
        }

        _seatLookup[obj.UserId].SetReady(obj.IsReady);

        var isRoomReady = _currentRoom.State.Users.Count(x => x.IsReady) == RoomsUtility.RoomCapacity(_currentRoom);

        if (isRoomReady)
            _ = StartCoroutine(InitiateMatchmakingAfterSeconds(MatchmakingCountdownSeconds));
    }

    private void ManageRoomName(RoomNameChangedArgs obj)
    {
        roomViewElements.RoomName.text = obj.RoomName;
    }

    private void ManageRoomPrivacy(RoomPublicnessChangedArgs obj)
    {
        roomViewElements.SetPrivacy(obj.IsPrivate);
    }

    private void ManageAdditionalData(CustomMatchmakingDataChangedArgs obj)
    {
        roomViewElements.TrySetSampleData(_currentRoom);
    }

    private void OnMatchmakingStateChanged(MatchmakingDataChangedArgs obj)
    {
        var currentState = _currentRoom.State.MatchmakingData.MatchmakingState;

        Debug.Log(currentState);

        if (currentState == MatchmakingState.RequestingMatchmaking)
            statusText.text = RoomStatusMessages.MatchmakingStartedMessage;
        else if (currentState == MatchmakingState.Matched)
            statusText.text = RoomStatusMessages.MatchmakingFinishedMessage;
    }

    private void OnMatchmakingEnded(MatchmakingEndedArgs obj)
    {
        if (_currentRoom.State.MatchmakingData.MatchmakingState == MatchmakingState.Playing)
            return;

        statusText.text = string.Format(RoomStatusMessages.MatchmakingErrorMessage, _currentRoom.State.MatchmakingData.MatchData?.FailReason);

        LockPlayersInRoom(false);
    }

    private IEnumerator InitiateMatchmakingAfterSeconds(int delay)
    {
        LockPlayersInRoom(true);

        for (var i = delay; i > 0; i--)
        {
            statusText.text = string.Format(RoomStatusMessages.MatchmakingStartCountdownMessage, i);
            yield return new WaitForSeconds(1);
        }

        if (AmIHost)
            _ = _currentRoom.StartMatchmaking();
    }

    private void LockPlayersInRoom(bool shouldBeLocked)
    {
        leaveButton.interactable = !shouldBeLocked;
        _seatLookup[MyUserId].LockUnreadyInteractability(shouldBeLocked);

        if (AmIHost)
            roomViewElements.ManageInteractability(!shouldBeLocked);
    }

    public override void Show()
    {
        _currentRoom ??= RoomsUtility.RoomsManager.ListJoinedRooms().First();

        roomViewElements.RoomName.text = _currentRoom.State.RoomName;
        roomViewElements.SetPrivacy(_currentRoom.State.IsPrivate);
        roomViewElements.TrySetSampleData(_currentRoom);

        joinCode.text = _currentRoom.State.JoinCode;

        var users = _currentRoom.State.Users;

        for (var i = 0; i < users.Count; i++)
        {
            _ = TryTakeSeat(playerSeats[i], users[i].UserId);
            playerSeats[i].SetReady(users[i].IsReady);
        }

        _seatLookup[_currentRoom.State.Host.UserId].SetHostIndicator();
        roomViewElements.ManageInteractability(AmIHost);

        readyButton.interactable = true;
        ManageRoomFill(users.Count);

        SetVisibility(true);
    }

    public override void Hide()
    {
        SetVisibility(false);

        leavePopup.Hide();

        foreach (var seat in _seatLookup.Values)
            seat.SetEmpty();
        _seatLookup.Clear();

        _ = _currentRoom.Leave();
    }

    private void SetVisibility(bool shouldBeVisible)
    {
        roomCanvasGroup.alpha = shouldBeVisible ? 1 : 0;
        roomCanvasGroup.blocksRaycasts = shouldBeVisible;
    }

    [UsedImplicitly]
    public void SendReadinessState(bool shouldMarkReady)
    {
        if (shouldMarkReady)
            _ = _currentRoom.MarkYourselfReady(null, null);
        else
            _ = _currentRoom.MarkYourselfUnready();
    }

    [UsedImplicitly]
    public async void SaveDataChange()
    {
        try
        {
            if (!AmIHost || !IsActive)
                return;

            var customMatchmakingData = new Dictionary<string, string>(_currentRoom.State.MatchmakingData.CustomData)
            {
                [RoomsUtility.SampleDataKey] = roomViewElements.SampleGameData.text
            };
            await _currentRoom.UpdateRoomParams(roomViewElements.RoomName.text, roomViewElements.IsPrivate, null, customMatchmakingData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error when updating room params: {e.Message}");
        }
    }

    [UsedImplicitly]
    public void LeaveAndShowRoomChoiceView() => RoomsNavigationController.Instance.ShowRoomChoiceView();
}
