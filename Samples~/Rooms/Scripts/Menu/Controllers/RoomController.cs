using System.Collections.Generic;
using UnityEngine;
using Elympics;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using System.Collections;
using JetBrains.Annotations;
using Elympics.Rooms.Models;

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

    private readonly Dictionary<Guid, PlayerSeat> seatLookup = new();
    private IRoom currentRoom;

    private static Guid MyUserId => ElympicsLobbyClient.Instance.AuthData.UserId;
    private bool AmIHost => MyUserId.Equals(currentRoom.State.Host.UserId);
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
        RoomsUtility.RoomsManager.CustomMatchmakingDataChanged += ManageAdditionalData;

        RoomsUtility.RoomsManager.MatchmakingEnded += OnMatchmakingEnded;
        RoomsUtility.RoomsManager.MatchmakingDataChanged += OnMatchmakingStateChanged;

        RoomsUtility.RoomsManager.StartTrackingAvailableRooms();
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
        RoomsUtility.RoomsManager.CustomMatchmakingDataChanged -= ManageAdditionalData;

        RoomsUtility.RoomsManager.MatchmakingEnded -= OnMatchmakingEnded;
        RoomsUtility.RoomsManager.MatchmakingDataChanged -= OnMatchmakingStateChanged;

        try
        {
            RoomsUtility.RoomsManager.StopTrackingAvailableRooms();
        }
        catch { }
    }
    #endregion

    private void SetUpRoomData(JoinedRoomArgs obj)
    {
        if (!RoomsUtility.RoomsManager.TryGetJoinedRoom(obj.RoomId, out currentRoom))
            Debug.LogError("Joined room not found!");

        RoomsNavigationController.Instance.ShowRoomView();

        var firstUser = currentRoom.State.Users.First();
        currentRoom.ChangeTeam(firstUser.TeamIndex.HasValue ? 1 - firstUser.TeamIndex : 0);
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
        seatLookup[userId] = seat;
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
        seatLookup[obj.User.UserId].SetEmpty();
        seatLookup.Remove(obj.User.UserId);
    }

    private void ManageRoomFill(UserCountChangedArgs obj) => ManageRoomFill((int)obj.UserCount);

    private void ManageRoomFill(int userCount)
    {
        bool allSeatsFull = userCount == RoomsUtility.RoomCapacity(currentRoom);

        readyButton.gameObject.SetActive(allSeatsFull);
        statusText.text = allSeatsFull ? RoomStatusMessages.WaitingForReadyMessage : RoomStatusMessages.WaitingForPlayerToJoinMessage;
    }

    private void ManageHostIndicatorState(HostChangedArgs obj)
    {
        var oldSeat = seatLookup[obj.UserId];

        if (TryTakeSeat(playerSeats.First(), obj.UserId))
        {
            oldSeat.SetEmpty();

            if (MyUserId.Equals(obj.UserId))
            {
                roomViewElements.ManageInteractability(true);
            }
        }

        seatLookup[obj.UserId].SetHostIndicator();
    }

    private void ManageReadiness(UserReadinessChangedArgs obj)
    {
        if (!obj.IsReady)
            LockPlayersInRoom(false);

        if (MyUserId.Equals(obj.UserId))
        {
            readyButton.interactable = !obj.IsReady;
        }

        seatLookup[obj.UserId].SetReady(obj.IsReady);

        var isRoomReady = currentRoom.State.Users.Count(x => x.IsReady) == RoomsUtility.RoomCapacity(currentRoom);

        if (isRoomReady)
            StartCoroutine(InitiateMatchmakingAfterSeconds(MatchmakingCountdownSeconds));
    }

    private void ManageAdditionalData(CustomMatchmakingDataChangedArgs obj)
    {
        roomViewElements.TrySetSampleData(currentRoom);
    }

    private void OnMatchmakingStateChanged(MatchmakingDataChangedArgs obj)
    {
        var currentState = currentRoom.State.MatchmakingData.MatchmakingState;

        if (currentState == MatchmakingState.RequestingMatchmaking)
            statusText.text = RoomStatusMessages.MatchmakingStartedMessage;
        else if (currentState == MatchmakingState.Matched)
            statusText.text = RoomStatusMessages.MatchmakingFinishedMessage;
    }

    private void OnMatchmakingEnded(MatchmakingEndedArgs obj)
    {
        if (currentRoom.State.MatchmakingData.MatchmakingState == MatchmakingState.Playing)
            return;

        statusText.text = string.Format(RoomStatusMessages.MatchmakingErrorMessage, currentRoom.State.MatchmakingData.MatchData?.FailReason);

        LockPlayersInRoom(false);
    }

    private IEnumerator InitiateMatchmakingAfterSeconds(int delay)
    {
        LockPlayersInRoom(true);

        for (int i = delay; i > 0; i--)
        {
            statusText.text = string.Format(RoomStatusMessages.MatchmakingStartCountdownMessage, i);
            yield return new WaitForSeconds(1);
        }

        if (MyUserId.Equals(currentRoom.State.Host))
            currentRoom.StartMatchmaking();
    }

    private void LockPlayersInRoom(bool shouldBeLocked)
    {
        leaveButton.interactable = !shouldBeLocked;
        seatLookup[MyUserId].LockUnreadyInteractability(shouldBeLocked);

        if (AmIHost)
            roomViewElements.ManageInteractability(!shouldBeLocked);
    }

    public override void Show()
    {
        if (currentRoom == null)
            currentRoom = RoomsUtility.RoomsManager.ListJoinedRooms().First();

        roomViewElements.RoomName.text = currentRoom.State.RoomName;
        roomViewElements.SetPrivacy(currentRoom.State.IsPrivate);
        roomViewElements.TrySetSampleData(currentRoom);

        joinCode.text = currentRoom.State.JoinCode;

        var users = currentRoom.State?.Users;
        if (users == null)
            throw new Exception("User list of the room is null!");

        for (int i = 0; i < users.Count; i++)
        {
            TryTakeSeat(playerSeats[i], users[i].UserId);
            playerSeats[i].SetReady(users[i].IsReady);
        }

        seatLookup[currentRoom.State.Host.UserId].SetHostIndicator();
        roomViewElements.ManageInteractability(AmIHost);

        readyButton.interactable = true;
        ManageRoomFill(users.Count);

        SetVisibility(true);
    }

    public override void Hide()
    {
        SetVisibility(false);

        leavePopup.Hide();

        foreach (var seat in seatLookup.Values)
            seat.SetEmpty();
        seatLookup.Clear();

        currentRoom.Leave();
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
            currentRoom.MarkYourselfReady(null, null);
        else
            currentRoom.MarkYourselfUnready();
    }

    [UsedImplicitly]
    public async void SaveDataChange()
    {
        try
        {
            if (!AmIHost || !IsActive)
                return;

            var customMatchmakingData = new Dictionary<string, string>(currentRoom.State.MatchmakingData.CustomData)
            {
                [RoomsUtility.SampleDataKey] = roomViewElements.SampleGameData.text
            };
            await currentRoom.UpdateRoomParams(roomViewElements.RoomName.text, roomViewElements.IsPrivate, null, customMatchmakingData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error when updating room params: {e.Message}");
        }
    }

    [UsedImplicitly]
    public void LeaveAndShowRoomChoiceView() => RoomsNavigationController.Instance.ShowRoomChoiceView();
}
