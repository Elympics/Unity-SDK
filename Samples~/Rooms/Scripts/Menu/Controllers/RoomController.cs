using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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
    [SerializeField] private CanvasGroup joinCodeSection;
    [SerializeField] private CanvasGroup joinIdSection;
    [SerializeField] private TextMeshProUGUI joinCode;
    [SerializeField] private TextMeshProUGUI joinId;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button startMatchmakingButton;
    [SerializeField] private BasePopup leavePopup;

    [SerializeField] private RoomWindowView roomViewElements;
    [SerializeField] private AdditionalRoomData _additionalRoomData;
    private PlayerSeat[] _playerSeats;
    private Dictionary<int, Guid[]> _teams = new();

    private bool IsActive => roomCanvasGroup.blocksRaycasts;
    private IRoom FirstJoinedRoom => RoomsUtility.RoomsManager.ListJoinedRooms().FirstOrDefault();

    private int RoomCapacity => RoomsUtility.RoomCapacity(_currentRoom);

    private readonly Dictionary<Guid, PlayerSeat> _seatLookup = new();
    private IRoom _currentRoom;
    private static Guid MyUserId => ElympicsLobbyClient.Instance.AuthData.UserId;
    private bool AmIHost => MyUserId.Equals(_currentRoom.State.Host.UserId);

    [SerializeField] private Button recconectButton;

    public void InitController()
    {
        recconectButton.gameObject.SetActive(false);
        roomViewElements.Init(_additionalRoomData.GetDataHolderUi());
        roomViewElements.SubscribeRoomNameChenge(SaveDataChanged);
        ElympicsLobbyClient.Instance.ShouldLoadGameplaySceneAfterMatchmaking = true;
        _ = StartCoroutine(TrackListAfterWait());
        RejoinRoomIfAvailable();
    }
    public void DeinitController()
    {
        roomViewElements.UnsubscribeRoomNameChanged();
        ElympicsLobbyClient.Instance.ShouldLoadGameplaySceneAfterMatchmaking = false;
        _ = RoomsUtility.RoomsManager.StopTrackingAvailableRooms();
    }
    private void RejoinRoomIfAvailable()
    {
        if (RoomsUtility.LastConnectedRoom == Guid.Empty)
            return;
        InitRejoinRoom();
    }
    private void Init()
    {
        CreateSeats();
        CreateTeams();
        AssignJoinedPlayersToSeats();
        AssignJoinedPlayersToTeams();
        _seatLookup[_currentRoom.State.Host.UserId].SetHostIndicator();
        SetJoinCode();
        SetIdCode();
    }
    public async void InitRoom(Guid RoomId)
    {
        if (!RoomsUtility.RoomsManager.TryGetJoinedRoom(RoomId, out _currentRoom))
            return;

        Init();

        var firstUser = _currentRoom.State.Users.First();
        if (!AssignPlayerToEmptyTeam(firstUser, out var emptyTeam))
            return;
        await _currentRoom.ChangeTeam((uint?)emptyTeam);
        UpdateSeatsVisuals();
        SetTeamsColors();
        RoomsNavigationController.Instance.ShowRoomView();
    }
    public void Recconect(Guid RoomId)
    {
        if (!RoomsUtility.RoomsManager.TryGetJoinedRoom(RoomId, out _currentRoom))
            Debug.LogError("Room doesnt exist");

        Init();

        UpdateSeatsVisuals();
        SetTeamsColors();
        RoomsNavigationController.Instance.ShowRoomView();
        var state = _currentRoom.State.MatchmakingData.MatchmakingState;

        if (state == MatchmakingState.Unlocked)
        {
            readyButton.gameObject.SetActive(true);
            return;
        }

        recconectButton.onClick.AddListener(() =>
        {
            RoomsUtility.LastConnectedRoom = _currentRoom.RoomId;
            UnsubscribeFromRoomEvents();
            _currentRoom.PlayAvailableMatch();
        });
        ForceLockPlayersInRoom(true);
        recconectButton.gameObject.SetActive(true);
        statusText.text = RoomStatusMessages.RecconectMessage;

    }
    private void InitRejoinRoom()
    {
        if (!RoomsUtility.RoomsManager.TryGetJoinedRoom(RoomsUtility.LastConnectedRoom, out var lastConnectedRoom))
            return;
        _currentRoom = lastConnectedRoom;
        Init();
        SetTeamsColors();

        if (_currentRoom.State.MatchmakingData.MatchmakingState == MatchmakingState.Playing)
        {
            recconectButton.gameObject.SetActive(true);
            statusText.text = RoomStatusMessages.RecconectMessage;

            ForceLockPlayersInRoom(true);
        }

        recconectButton.onClick.AddListener(() =>
        {
            UnsubscribeFromRoomEvents();
            ElympicsLobbyClient.Instance.RejoinLastOnlineMatch();
        });
        RoomsNavigationController.Instance.ShowRoomView();
    }
    private void UpdateSeatsVisuals()
    {
        foreach (var entry in _seatLookup)
        {
            UpdateSeatVisuals(entry.Value, entry.Key);
        }
    }
    private void UpdateSeatVisuals(PlayerSeat seat, Guid userId)
    {
        var user = GetCurrentRoomUserInfo(userId);
        seat.SetSeatText(user);
    }
    public void SubscribeToRoomEvents()
    {
        RoomsUtility.RoomsManager.UserJoined += AddPlayer;
        RoomsUtility.RoomsManager.UserLeft += RemovePlayer;
        RoomsUtility.RoomsManager.UserCountChanged += ManageRoomFill;
        RoomsUtility.RoomsManager.HostChanged += ManageHostIndicatorState;
        RoomsUtility.RoomsManager.UserReadinessChanged += ManageReadiness;
        RoomsUtility.RoomsManager.UserChangedTeam += SetTeamsColors;

        RoomsUtility.RoomsManager.RoomNameChanged += ManageRoomName;
        RoomsUtility.RoomsManager.RoomPublicnessChanged += ManageRoomPrivacy;

        RoomsUtility.RoomsManager.MatchmakingEnded += OnMatchmakingEnded;
        RoomsUtility.RoomsManager.MatchmakingDataChanged += OnMatchmakingStateChanged;
    }
    public void UnsubscribeFromRoomEvents()
    {

        RoomsUtility.RoomsManager.UserJoined -= AddPlayer;
        RoomsUtility.RoomsManager.UserLeft -= RemovePlayer;
        RoomsUtility.RoomsManager.UserCountChanged -= ManageRoomFill;
        RoomsUtility.RoomsManager.HostChanged -= ManageHostIndicatorState;
        RoomsUtility.RoomsManager.UserReadinessChanged -= ManageReadiness;
        RoomsUtility.RoomsManager.UserChangedTeam -= SetTeamsColors;

        RoomsUtility.RoomsManager.RoomNameChanged -= ManageRoomName;
        RoomsUtility.RoomsManager.RoomPublicnessChanged -= ManageRoomPrivacy;

        RoomsUtility.RoomsManager.MatchmakingEnded -= OnMatchmakingEnded;
        RoomsUtility.RoomsManager.MatchmakingDataChanged -= OnMatchmakingStateChanged;
    }

    public override void Show()
    {
        SubscribeToRoomEvents();

        roomViewElements.RoomName.text = _currentRoom.State.RoomName;
        roomViewElements.SetPrivacy(_currentRoom.State.IsPrivate);
        roomViewElements.ManageInteractability(AmIHost && _currentRoom.State.MatchmakingData.MatchmakingState != MatchmakingState.Playing);

        var users = _currentRoom.State.Users;

        readyButton.interactable = true;
        ManageRoomFill(users.Count);

        _additionalRoomData.PopulateData(_currentRoom);
        _additionalRoomData.UpdateUi();
        _additionalRoomData.SubscribeOnDataUpdate(new Action(SaveDataChanged));
        SetVisibility(true);
    }
    public override void Hide()
    {
        UnsubscribeFromRoomEvents();

        SetVisibility(false);
        leavePopup.Hide();

        foreach (var seat in _seatLookup.Values)
            seat.SetEmpty();
        _seatLookup.Clear();

        _additionalRoomData.ClearData();
        _additionalRoomData.UnsubscribeOnDataUpdate();
        DestroySeats();
        _currentRoom.Leave().Forget();
    }
    public void CreateSeats()
    {
        _playerSeats = new PlayerSeat[RoomsUtility.RoomCapacity(_currentRoom)];
        for (var i = 0; i < _playerSeats.Length; i++)
        {
            var newSeat = roomViewElements.GetPlayerSeat();
            newSeat.InitButton(SetUnreadyState);
            _playerSeats[i] = newSeat;
            newSeat.transform.SetParent(roomViewElements.GetSeatsHolder());
            newSeat.transform.localScale = Vector3.one;
        }
    }
    public void CreateTeams()
    {
        _teams = new Dictionary<int, Guid[]>();
        var teamCount = _currentRoom.State.MatchmakingData.TeamCount;
        var teamSize = _currentRoom.State.MatchmakingData.TeamSize;

        for (var i = 0; i < teamCount; i++)
        {
            _teams.Add(i, new Guid[teamSize]);
        }
    }
    private void AssignJoinedPlayersToSeats()
    {
        _seatLookup.Clear();
        var conectedUsers = _currentRoom.State.Users;
        foreach (var user in conectedUsers)
        {
            var freeSeat = GetFreeSeat() ?? throw new Exception("No free seats");
            AssignPlayer(user, freeSeat);
        }
    }
    private void AssignJoinedPlayersToTeams()
    {
        var conectedUsers = _currentRoom.State.Users;
        foreach (var user in conectedUsers)
        {
            if (user.TeamIndex != null)
            {
                var team = _teams[(int)user.TeamIndex];
                for (var i = 0; i < team.Length; i++)
                {
                    if (team[i] == Guid.Empty)
                    {
                        team[i] = user.UserId;
                        break;
                    }
                }
            }
        }
    }
    private void AssignPlayer(UserInfo player, PlayerSeat seat)
    {
        _seatLookup.Add(player.UserId, seat);
        seat.SetPlayer(player, MyUserId.Equals(player.UserId));
    }
    public bool AssignPlayerToEmptyTeam(UserInfo player, out int emptyTeam)
    {
        foreach (var team in _teams)
        {
            for (var i = 0; i < team.Value.Length; i++)
            {
                if (team.Value[i] == Guid.Empty)
                {
                    team.Value[i] = player.UserId;
                    emptyTeam = team.Key;
                    return true;
                }
            }
        }
        emptyTeam = -1;
        return false;
    }
    private PlayerSeat GetFreeSeat()
    {
        foreach (var seat in _playerSeats)
        {
            if (!seat.IsOccupied)
            {
                return seat;
            }
        }
        return null;
    }
    public void SetTeamsColors(UserChangedTeamArgs usr) => SetTeamsColors();
    private void SetTeamsColors()
    {
        var users = _currentRoom.State.Users;
        foreach (var user in users)
        {
            if (user.TeamIndex == null)
                continue;
            var teamColor = new Color((float)user.TeamIndex / _teams.Count, (float)user.TeamIndex / _teams.Count, (float)user.TeamIndex / _teams.Count);
            _seatLookup[user.UserId].SetTeamColor(teamColor);
        }
    }
    #region Events integration
    #endregion
    private IEnumerator TrackListAfterWait()
    {
        yield return new WaitUntil(() => ElympicsLobbyClient.Instance.IsAuthenticated && ElympicsLobbyClient.Instance.WebSocketSession.IsConnected);
        Debug.Log("Initialized room list tracking");
        _ = RoomsUtility.RoomsManager.StartTrackingAvailableRooms();
    }
    private void RoomLeaveFeedback(LeftRoomArgs _)
    {
        startMatchmakingButton.gameObject.SetActive(false);
        Debug.Log("You've just left the room.");
    }
    private void AddPlayer(UserJoinedArgs obj) => AddPlayer(obj.User);
    private void AddPlayer(UserInfo player)
    {
        var freeSeat = GetFreeSeat();
        if (freeSeat == null)
            Debug.LogError("No free seats!");
        AssignPlayer(player, freeSeat);
    }
    private void RemovePlayer(UserLeftArgs obj)
    {
        _seatLookup[obj.User.UserId].SetEmpty();
        _ = _seatLookup.Remove(obj.User.UserId);
    }

    private void ManageRoomFill(UserCountChangedArgs obj) => ManageRoomFill((int)obj.UserCount);

    private void ManageRoomFill(int userCount)
    {
        if (_currentRoom == null)
            return;
        var allSeatsFull = userCount == RoomsUtility.RoomCapacity(_currentRoom);

        readyButton.gameObject.SetActive(allSeatsFull && !_currentRoom.State.Users.Single(x => x.UserId == MyUserId).IsReady);
        statusText.text = allSeatsFull ? RoomStatusMessages.WaitingForReadyMessage : RoomStatusMessages.WaitingForPlayerToJoinMessage;
    }
    private void ManageHostIndicatorState(HostChangedArgs obj)
    {
        var oldSeat = _seatLookup[obj.UserId];
        var newHost = GetCurrentRoomUserInfo(obj.UserId);

        if (_playerSeats[0] != null && !_playerSeats[0].IsOccupied)
        {
            if (!_seatLookup.TryGetValue(newHost.UserId, out var _))
            {
                AssignPlayer(newHost, _playerSeats[0]);
            }
            else
            {
                _playerSeats[0].SetPlayer(newHost, MyUserId.Equals(newHost.UserId));
            }
            oldSeat.SetEmpty();

            if (MyUserId.Equals(obj.UserId))
            {
                SetJoinCode();
                SetIdCode();
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

        if (isRoomReady && AmIHost)
            startMatchmakingButton.gameObject.SetActive(true);
        else
            startMatchmakingButton.gameObject.SetActive(false);
    }
    private void ManageRoomName(RoomNameChangedArgs obj) => roomViewElements.RoomName.text = obj.RoomName;

    private void ManageRoomPrivacy(RoomPublicnessChangedArgs obj) => roomViewElements.SetPrivacy(obj.IsPrivate);
    private void OnMatchmakingStateChanged(MatchmakingDataChangedArgs obj)
    {
        var currentState = _currentRoom.State.MatchmakingData.MatchmakingState;
        if (currentState == MatchmakingState.Matchmaking)
            statusText.text = RoomStatusMessages.MatchmakingStartedMessage;
        else if (currentState == MatchmakingState.Matched)
        {
            UnsubscribeFromRoomEvents();
            RoomsUtility.LastConnectedRoom = _currentRoom.RoomId;
            statusText.text = RoomStatusMessages.MatchmakingFinishedMessage;
        }
        else if (currentState == MatchmakingState.Playing)
        {
            ForceLockPlayersInRoom(true);
        }
        else if (currentState == MatchmakingState.Unlocked)
        {
            LockPlayersInRoom(false);
            recconectButton.gameObject.SetActive(false);
            readyButton.gameObject.SetActive(true);
        }
    }

    private void OnMatchmakingEnded(MatchmakingEndedArgs obj)
    {
        var roomMatchmakingData = _currentRoom.State.MatchmakingData;
        if (roomMatchmakingData.MatchmakingState == MatchmakingState.Playing || roomMatchmakingData?.MatchData?.FailReason == null)
            return;

        statusText.text = string.Format(RoomStatusMessages.MatchmakingErrorMessage, roomMatchmakingData.MatchData?.FailReason);

        LockPlayersInRoom(false);
    }
    [UsedImplicitly]
    public void StartMatchmaking()
    {
        startMatchmakingButton.gameObject.SetActive(false);
        _ = StartCoroutine(InitiateMatchmakingAfterSeconds(MatchmakingCountdownSeconds));
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
            _currentRoom.StartMatchmaking().Forget();
    }

    private void LockPlayersInRoom(bool shouldBeLocked)
    {
        leaveButton.interactable = !shouldBeLocked;
        _seatLookup[MyUserId].LockUnreadyInteractability(shouldBeLocked);

        if (AmIHost)
            roomViewElements.ManageInteractability(!shouldBeLocked);
    }
    private void ForceLockPlayersInRoom(bool shouldBeLocked)
    {
        leaveButton.interactable = !shouldBeLocked;
        _seatLookup[MyUserId].LockUnreadyInteractability(shouldBeLocked);
        roomViewElements.ManageInteractability(!shouldBeLocked);
    }
    public void DestroySeats()
    {
        if (_playerSeats == null || _playerSeats.Length == 0)
            return;

        foreach (var seat in _playerSeats)
        {
            Destroy(seat.gameObject);
        }
    }
    private void SetVisibility(bool shouldBeVisible)
    {
        roomCanvasGroup.alpha = shouldBeVisible ? 1 : 0;
        roomCanvasGroup.blocksRaycasts = shouldBeVisible;
    }

    private void SetJoinCode()
    {
        joinCodeSection.alpha = _currentRoom.State.JoinCode != null ? 1 : 0;
        joinCode.text = _currentRoom.State.JoinCode;
    }
    private void SetIdCode()
    {
        joinIdSection.alpha = _currentRoom.RoomId != null ? 1 : 0;
        joinId.text = _currentRoom.RoomId.ToString();
    }
    [UsedImplicitly]
    public void SetReadyState() => SetReadyStateAsync().Forget();
    public async UniTaskVoid SetReadyStateAsync()
    {
        LockPlayersInRoom(true);
        readyButton.gameObject.SetActive(false);
        startMatchmakingButton.gameObject.SetActive(false);

        await _currentRoom.MarkYourselfReady();
        LockPlayersInRoom(false);
    }
    private void SetUnreadyState() => SetUnreadyStateAsync().Forget();
    public async UniTaskVoid SetUnreadyStateAsync()
    {
        LockPlayersInRoom(true);
        if (startMatchmakingButton.gameObject.activeSelf)
            startMatchmakingButton.gameObject.SetActive(false);
        await _currentRoom.MarkYourselfUnready();
        readyButton.gameObject.SetActive(true);
        LockPlayersInRoom(false);
    }
    private UserInfo GetCurrentRoomUserInfo(Guid userId)
    {
        var users = _currentRoom.State.Users;
        return users.Where(user => user.UserId == userId).DefaultIfEmpty(null).First();
    }
    [UsedImplicitly]
    public void SaveDataChanged() => SaveDataChangeAsync().Forget();
    private async UniTask SaveDataChangeAsync()
    {
        try
        {
            if (!AmIHost || !IsActive)
                return;

            await _currentRoom.UpdateRoomParams(roomViewElements.RoomName.text, roomViewElements.IsPrivate, _additionalRoomData.GetCustomRoomData(), _additionalRoomData.GetMatchmakingRoomData());//TODO keys
        }
        catch (Exception e)
        {
            Debug.LogError($"Error when updating room params: {e.Message}");
        }
    }

    [UsedImplicitly]
    public void LeaveAndShowRoomChoiceView() => RoomsNavigationController.Instance.ShowRoomChoiceView();
}
