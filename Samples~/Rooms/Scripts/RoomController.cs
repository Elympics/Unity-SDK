using System.Collections.Generic;
using UnityEngine;
using Elympics;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using System.Collections;
using JetBrains.Annotations;

public class RoomController : BaseWindow
{
    private const int MatchmakingCountdownSeconds = 3;

    private const string WaitingForPlayerToJoinMessage = "Waiting for another player to join...";
    private const string WaitingForReadyMessage = "Waiting for all players to be ready...";
    private const string MatchmakingStartCountdownMessage = "Matchmaking starts in {}";
    private const string MatchmakingStartedMessage = "Matchmaking started! Waiting for servers allocation...";
    private const string MatchmakingFinishedMessage = "Match is ready!";
    private const string MatchmakingErrorMessage = "Matchmaking failed because of an error: {}";

    [SerializeField] private CanvasGroup roomCanvasGroup;
    [SerializeField] private RoomViewBaseElements roomViewElements;
    [SerializeField] private TextMeshProUGUI joinCode;
    [SerializeField] private Selectable[] hostInteractableElements;

    [SerializeField] private Button readyButton;
    [SerializeField] private Button unreadyButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private PlayerSeat[] playerSeats;
    [SerializeField] private TextMeshProUGUI statusText;

    private readonly Dictionary<Guid, PlayerSeat> seatLookup = new();
    private IRoom currentRoom;

    private static Guid MyUserId => ElympicsLobbyClient.Instance.AuthData.UserId;
    private bool AmIHost => MyUserId.Equals(currentRoom.State.Host.UserId);

    private void Awake()
    {
        SubscribeToRoomEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromRoomEvents();
    }

    private void SubscribeToRoomEvents()
    {
        RoomsUtility.RoomsManager.RoomStateChanged += RefreshRoomData;

        RoomsUtility.RoomsManager.JoinedRoom += SetUpRoomData;
        RoomsUtility.RoomsManager.LeftRoom += RoomLeaveFeedback;

        RoomsUtility.RoomsManager.UserJoined += AddPlayer;
        RoomsUtility.RoomsManager.UserLeft += RemovePlayer;
        RoomsUtility.RoomsManager.UserCountChanged += ManageRoomFill;
        RoomsUtility.RoomsManager.HostChanged += ManageHostIndicatorState;
        RoomsUtility.RoomsManager.UserReadinessChanged += ManageReadiness;
        RoomsUtility.RoomsManager.RoomParametersChanged += ManageAddtionalData;

        RoomsUtility.RoomsManager.MatchmakingStarted += OnMatchmakingStarted;
        RoomsUtility.RoomsManager.MatchmakingSucceeded += OnMatchmakingSucceeded;
        RoomsUtility.RoomsManager.MatchmakingFailed += OnMatchmakingFailed;
    }

    private void UnsubscribeFromRoomEvents()
    {
        RoomsUtility.RoomsManager.RoomStateChanged -= RefreshRoomData;

        RoomsUtility.RoomsManager.JoinedRoom -= SetUpRoomData;
        RoomsUtility.RoomsManager.LeftRoom -= RoomLeaveFeedback;

        RoomsUtility.RoomsManager.UserJoined -= AddPlayer;
        RoomsUtility.RoomsManager.UserLeft -= RemovePlayer;
        RoomsUtility.RoomsManager.UserCountChanged -= ManageRoomFill;
        RoomsUtility.RoomsManager.HostChanged -= ManageHostIndicatorState;
        RoomsUtility.RoomsManager.UserReadinessChanged -= ManageReadiness;
        RoomsUtility.RoomsManager.RoomParametersChanged -= ManageAddtionalData;

        RoomsUtility.RoomsManager.MatchmakingStarted -= OnMatchmakingStarted;
        RoomsUtility.RoomsManager.MatchmakingSucceeded -= OnMatchmakingSucceeded;
        RoomsUtility.RoomsManager.MatchmakingFailed -= OnMatchmakingFailed;
    }

    private void RefreshRoomData(RoomStateChangedArgs obj)
    {
        Debug.Log("Refreshed room data");
    }

    private void SetUpRoomData(JoinedRoomArgs obj)
    {
        if (!RoomsUtility.RoomsManager.TryGetJoinedRoom(obj.RoomId, out currentRoom))
            Debug.LogError("Joined room not found!");

        Show();

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
                ManageInteractiveness(true);
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

    private void ManageAddtionalData(RoomParametersChangedArgs obj)
    {
        roomViewElements.SampleGameData.text = currentRoom.State.RoomParameters[RoomsUtility.SampleDataKey];
    }

    private void OnMatchmakingStarted(MatchmakingStartedArgs obj)
    {
        statusText.text = MatchmakingStartedMessage;
    }

    private void OnMatchmakingSucceeded(MatchmakingSucceededArgs obj)
    {
        statusText.text = MatchmakingFinishedMessage;
    }

    private void OnMatchmakingFailed(MatchmakingFailedArgs obj)
    {
        statusText.text = string.Format(MatchmakingErrorMessage, obj.Error);

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
            ManageInteractiveness(!shouldBeLocked);
    }

    public override void Show()
    {
        if (currentRoom == null)
            currentRoom = RoomsUtility.RoomsManager.ListJoinedRooms().First();

        roomViewElements.RoomName.text = currentRoom.RoomName;
        roomViewElements.RoomPrivacy.SelectOption(currentRoom.JoinCode == null ? 0 : 1); //TODO: use privacy property
        roomViewElements.SampleGameData.text = currentRoom.State.RoomParameters[RoomsUtility.SampleDataKey];

        joinCode.text = currentRoom.JoinCode;

        var users = currentRoom.State?.Users;
        if (users == null)
            throw new Exception("User list of the room is null!");

        for (int i = 0; i < users.Count; i++)
        {
            TryTakeSeat(playerSeats[i], users[i].UserId);
        }

        seatLookup[currentRoom.State.Host.UserId].SetHostIndicator();
        ManageInteractiveness(AmIHost);

        seatLookup[MyUserId].SetMyselfIndicator();
        unreadyButton.transform.parent = seatLookup[MyUserId].transform;

        SetVisibility(true);
    }

    public override void Hide()
    {
        SetVisibility(false);
        currentRoom.Leave();
    }

    private void ManageInteractiveness(bool shouldBeInteractive)
    {
        foreach (var element in hostInteractableElements)
        {
            element.interactable = shouldBeInteractive;
        }
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
}
