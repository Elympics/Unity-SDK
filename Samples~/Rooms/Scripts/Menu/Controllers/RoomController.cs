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
using Elympics.Models.Authentication;

public class RoomController : BaseWindow
{
    private const int MatchmakingCountdownSeconds = 3;

    private const string WaitingForPlayerToJoinMessage = "Waiting for another player to join...";
    private const string WaitingForReadyMessage = "Waiting for all players to be ready...";
    private const string MatchmakingStartCountdownMessage = "Matchmaking starts in {}";
    private const string MatchmakingStartedMessage = "Matchmaking started! Waiting for servers allocation...";
    private const string MatchmakingFinishedMessage = "Match is ready! Connecting to the server...";
    private const string MatchmakingErrorMessage = "Matchmaking failed because of an error: {}";

    [SerializeField] private CanvasGroup roomCanvasGroup;
    [SerializeField] private RoomViewBaseElements roomViewElements;
    [SerializeField] private TextMeshProUGUI joinCode;

    [SerializeField] private Button readyButton;
    [SerializeField] private Button unreadyButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private PlayerSeat[] playerSeats;
    [SerializeField] private TextMeshProUGUI statusText;

    private readonly Dictionary<Guid, PlayerSeat> seatLookup = new();
    private IRoom currentRoom;

    private static Guid MyUserId => ElympicsLobbyClient.Instance.AuthData.UserId;
    private bool AmIHost => MyUserId.Equals(currentRoom.State.Host.UserId);

    #region Events integration
    private void Awake()
    {
        Debug.Log(ElympicsLobbyClient.Instance.IsAuthenticated);

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

        RoomsUtility.RoomsManager.RoomStateChanged += RefreshRoomData;

        RoomsUtility.RoomsManager.JoinedRoom += SetUpRoomData;
        RoomsUtility.RoomsManager.LeftRoom += RoomLeaveFeedback;

        RoomsUtility.RoomsManager.UserJoined += AddPlayer;
        RoomsUtility.RoomsManager.UserLeft += RemovePlayer;
        RoomsUtility.RoomsManager.UserCountChanged += ManageRoomFill;
        RoomsUtility.RoomsManager.HostChanged += ManageHostIndicatorState;
        RoomsUtility.RoomsManager.UserReadinessChanged += ManageReadiness;
        RoomsUtility.RoomsManager.CustomDataChanged += ManageAdditionalData;

        RoomsUtility.RoomsManager.MatchmakingEnded += OnMatchmakingEnded;
        RoomsUtility.RoomsManager.MatchmakingStateChanged += OnMatchmakingStateChanged;
    }

    private void UnsubscribeFromRoomEvents()
    {
        if (!ElympicsLobbyClient.Instance.IsAuthenticated)
            return;

        RoomsUtility.RoomsManager.RoomStateChanged -= RefreshRoomData;

        RoomsUtility.RoomsManager.JoinedRoom -= SetUpRoomData;
        RoomsUtility.RoomsManager.LeftRoom -= RoomLeaveFeedback;

        RoomsUtility.RoomsManager.UserJoined -= AddPlayer;
        RoomsUtility.RoomsManager.UserLeft -= RemovePlayer;
        RoomsUtility.RoomsManager.UserCountChanged -= ManageRoomFill;
        RoomsUtility.RoomsManager.HostChanged -= ManageHostIndicatorState;
        RoomsUtility.RoomsManager.UserReadinessChanged -= ManageReadiness;
        RoomsUtility.RoomsManager.CustomDataChanged -= ManageAdditionalData;

        RoomsUtility.RoomsManager.MatchmakingEnded -= OnMatchmakingEnded;
        RoomsUtility.RoomsManager.MatchmakingStateChanged -= OnMatchmakingStateChanged;
    }
    #endregion

    private void RefreshRoomData(RoomStateChangedArgs obj)
    {
        Debug.Log("Refreshed room data");
    }

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

        seat.SetPlayer(userId);
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

    private void ManageRoomFill(UserCountChangedArgs obj)
    {
        bool allSeatsFull = obj.UserCount == RoomsUtility.MaxPlayers;

        readyButton.gameObject.SetActive(allSeatsFull);
        statusText.text = allSeatsFull ? WaitingForReadyMessage : WaitingForPlayerToJoinMessage;
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
                seatLookup[MyUserId].SetMyselfIndicator();
                unreadyButton.transform.parent = seatLookup[MyUserId].transform;
            }
        }

        seatLookup[obj.UserId].SetHostIndicator();
    }

    private void ManageReadiness(UserReadinessChangedArgs obj)
    {
        seatLookup[obj.UserId].SetReady(obj.IsReady);

        var isRoomFull = currentRoom.State.Users.Count(x => x.IsReady) == RoomsUtility.MaxPlayers;

        if (isRoomFull)
            StartCoroutine(InitiateMatchmakingAfterSeconds(MatchmakingCountdownSeconds));

        if (!obj.IsReady)
            LockPlayersInRoom(false);

        if (MyUserId.Equals(obj.UserId))
        {
            readyButton.interactable = !obj.IsReady;
            unreadyButton.interactable = !obj.IsReady;
        }
    }

    private void ManageAdditionalData(CustomDataChangedArgs obj)
    {
        roomViewElements.SampleGameData.text = obj.CustomData[RoomsUtility.SampleDataKey];
    }

    private void OnMatchmakingStateChanged(MatchmakingStateChangedArgs obj)
    {
        var currentState = currentRoom.State.MatchmakingData.MatchmakingState;

        if (currentState == MatchmakingState.RequestingMatchmaking)
            statusText.text = MatchmakingStartedMessage;
        else if (currentState == MatchmakingState.Matched)
            statusText.text = MatchmakingFinishedMessage;
    }

    private void OnMatchmakingEnded(MatchmakingEndedArgs obj)
    {
        if (currentRoom.State.MatchmakingData.MatchmakingState == MatchmakingState.Playing)
            return;

        statusText.text = string.Format(MatchmakingErrorMessage, currentRoom.State.MatchmakingData.MatchData?.FailReason);

        LockPlayersInRoom(false);
    }

    private IEnumerator InitiateMatchmakingAfterSeconds(int delay)
    {
        LockPlayersInRoom(true);

        for (int i = delay; i > 0; i--)
        {
            statusText.text = string.Format(MatchmakingStartCountdownMessage, i);
            yield return new WaitForSeconds(1);
        }

        if (MyUserId.Equals(currentRoom.State.Host))
            currentRoom.StartMatchmaking();
    }

    private void LockPlayersInRoom(bool shouldBeLocked)
    {
        leaveButton.interactable = !shouldBeLocked;
        unreadyButton.interactable = !shouldBeLocked;

        if (AmIHost)
            roomViewElements.ManageInteractability(!shouldBeLocked);
    }

    public override void Show()
    {
        if (currentRoom == null)
            currentRoom = RoomsUtility.RoomsManager.ListJoinedRooms().First();

        roomViewElements.RoomName.text = currentRoom.State.RoomName;
        roomViewElements.RoomPrivacy.SelectOption(currentRoom.State.JoinCode == null ? 0 : 1); //TODO: use privacy property
        roomViewElements.SampleGameData.text = currentRoom.State.MatchmakingData.CustomData[RoomsUtility.SampleDataKey];

        joinCode.text = currentRoom.State.JoinCode;

        var users = currentRoom.State?.Users;
        if (users == null)
            throw new Exception("User list of the room is null!");

        for (int i = 0; i < users.Count; i++)
        {
            TryTakeSeat(playerSeats[i], users[i].UserId);
        }

        seatLookup[currentRoom.State.Host.UserId].SetHostIndicator();
        roomViewElements.ManageInteractability(AmIHost);

        seatLookup[MyUserId].SetMyselfIndicator();
        unreadyButton.transform.parent = seatLookup[MyUserId].transform;

        SetVisibility(true);
    }

    public override void Hide()
    {
        SetVisibility(false);
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

        readyButton.interactable = !shouldMarkReady;
    }

    [UsedImplicitly]
    public void SaveAdditionalDataChange()
    {
        // TODO: when sdk has suitable method
        Debug.Log($"Sent {roomViewElements.SampleGameData.text} as new additional data");
    }

    [UsedImplicitly]
    public void LeaveAndShowRoomChoiceView() => RoomsNavigationController.Instance.ShowRoomChoiceView();
}
