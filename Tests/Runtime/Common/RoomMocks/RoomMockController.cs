using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Elympics
{
    [RequireComponent(typeof(ElympicsLobbyClient))]
    public class RoomMockController : MonoBehaviour
    {
        [Serializable]
        internal struct StringStringPair
        {
            public string key;
            public string value;

            private KeyValuePair<string, string> ToKeyValuePair() => new(key, value);
            public static Dictionary<string, string> ToDictionary(IEnumerable<StringStringPair> stringStringPairs) => new(stringStringPairs.Select(x => x.ToKeyValuePair()));
        }

        [SerializeField] private bool subscribeToEvents = true;

        [Header("Create and Join")]
        [SerializeField] private string roomName;
        [SerializeField] private string queueName;
        [SerializeField] private bool isSingleTeam;
        [SerializeField] private bool isPrivate;
        [SerializeField] private StringStringPair[] customRoomData;
        [SerializeField] private StringStringPair[] customMatchmakingData;

        [Header("Join")]
        [SerializeField] private string roomId;
        [SerializeField] private string joinCode;
        [SerializeField] private uint teamIndex;

        [Header("ChangeTeam")]
        [SerializeField] private uint newTeamIndex;

        [Header("Set Room Parameters")]
        [SerializeField] internal string modifiedRoomId;
        [SerializeField] internal string newRoomName;
        [SerializeField] internal bool newIsPrivate;
        [SerializeField] internal StringStringPair[] newCustomRoomData;
        [SerializeField] internal StringStringPair[] newCustomMatchmakingData;

        public IRoomsManager RoomManager { get; private set; }

        private void Start()
        {
            RoomManager = ElympicsLobbyClient.Instance.RoomsManager;

            if (!subscribeToEvents)
                return;

            RoomManager.JoinedRoomUpdated += OnJoinedRoomUpdated;
            RoomManager.LeftRoom += OnLeftRoom;
            RoomManager.HostChanged += OnHostChanged;
            RoomManager.JoinedRoom += OnJoinedRoom;
            RoomManager.UserJoined += OnUserJoined;
            RoomManager.UserLeft += OnUserLeft;
            RoomManager.CustomRoomDataChanged += OnCustomDataChanged;
            RoomManager.UserChangedTeam += OnUserChangedTeam;
            RoomManager.UserCountChanged += OnUserCountChanged;
            RoomManager.UserReadinessChanged += OnUserReadinessChanged;

            RoomManager.MatchmakingStarted += OnMatchmakingStarted;
            RoomManager.MatchmakingEnded += OnMatchmakingEnded;
            RoomManager.MatchDataReceived += OnMatchDataReceived;
            RoomManager.MatchmakingDataChanged += OnMatchmakingDataChanged;

            _ = RoomManager.StartTrackingAvailableRooms();
        }
        private void OnMatchmakingDataChanged(MatchmakingDataChangedArgs args)
        {
            Debug.Log($"[RoomHandler] On matchmaking state changed {args}");
        }
        private void OnMatchDataReceived(MatchDataReceivedArgs args)
        {
            Debug.Log($"[RoomHandler] On MatchData received {args}");
        }
        private void OnMatchmakingEnded(MatchmakingEndedArgs args)
        {
            Debug.Log($"[RoomHandler] On matchmaking ended {args}");
        }
        private void OnMatchmakingStarted(MatchmakingStartedArgs args)
        {
            Debug.Log($"[RoomHandler] On matchmaking started {args}");
        }
        private void OnUserReadinessChanged(UserReadinessChangedArgs args)
        {
            Debug.Log($"[RoomHandler] On user readiness changed {args}");
        }
        private void OnUserCountChanged(UserCountChangedArgs args)
        {
            Debug.Log($"[RoomHandler] On user count change {args}");
        }
        private void OnUserChangedTeam(UserChangedTeamArgs args)
        {
            Debug.Log($"[RoomHandler] On user changed team {args}");
        }
        private void OnCustomDataChanged(CustomRoomDataChangedArgs args)
        {
            Debug.Log($"[RoomHandler] On custom data changed {args.RoomId}");
            Debug.Log($"Key: {args.Key} Value: {args.Value}");
        }
        private void OnUserLeft(UserLeftArgs args)
        {
            Debug.Log($"[RoomHandler] On user left {args}");
        }
        private void OnUserJoined(UserJoinedArgs args)
        {
            Debug.Log($"[RoomHandler] On user joined {args}");
        }
        private void OnJoinedRoom(JoinedRoomArgs args)
        {
            Debug.Log($"[RoomHandler] On joined room {args}");
        }
        private void OnHostChanged(HostChangedArgs args)
        {
            Debug.Log($"[RoomHandler] On host changed {args}");
        }
        private void OnLeftRoom(LeftRoomArgs args)
        {
            Debug.Log($"[RoomHandler] On Room Left {args}");
        }
        private void OnJoinedRoomUpdated(JoinedRoomUpdatedArgs args)
        {
            Debug.Log($"[RoomHandler] Joined room updated {args}");
        }

        [ContextMenu("CreateAndJoin")]
        public async void CreateAndJoin()
        {
            try
            {
                var result = await RoomManager.CreateAndJoinRoom(roomName, queueName, isSingleTeam, isPrivate, StringStringPair.ToDictionary(customRoomData), StringStringPair.ToDictionary(customMatchmakingData));
                Debug.Log($"[ContextMenu] Created and Joined Room with roomId: {result.RoomId}");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        [ContextMenu("JoinRoomById")]
        public async void JoinByRoomId()
        {
            try
            {
                var result = await RoomManager.JoinRoom(new Guid(roomId), null, teamIndex);
                Debug.Log($"[ContextMenu] Joined Room with roomId: {result.RoomId}");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        [ContextMenu("JoinRoomByJoinCode")]
        public async void JoinByJoinCode()
        {
            try
            {
                var result = await RoomManager.JoinRoom(null, joinCode, teamIndex);
                Debug.Log($"[ContextMenu] Joined Room with roomId: {result.RoomId}");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        [ContextMenu("Leave first room from the list")]
        public async void LeaveCurrentRoom()
        {
            try
            {
                if (RoomManager.ListJoinedRooms().Count > 0)
                {
                    var room = RoomManager.ListJoinedRooms()[0];
                    await room.Leave();
                    Debug.Log($"[ContextMenu] Room Left");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        [ContextMenu("ChangeTeam")]
        public async void ChangeTeam()
        {
            try
            {
                if (RoomManager.ListJoinedRooms().Count > 0)
                {
                    var room = RoomManager.ListJoinedRooms()[0];
                    await room.ChangeTeam(newTeamIndex);
                    Debug.Log($"[ContextMenu] Team changed.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        [ContextMenu("Change to observer")]
        public async void ChangeToObserver()
        {
            try
            {
                if (RoomManager.ListJoinedRooms().Count > 0)
                {
                    var room = RoomManager.ListJoinedRooms()[0];
                    await room.ChangeTeam(null);
                    Debug.Log($"[ContextMenu] Became observer.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        [ContextMenu("SetReady")]
        public async void SetReady()
        {
            try
            {
                if (RoomManager.ListJoinedRooms().Count > 0)
                {
                    var room = RoomManager.ListJoinedRooms()[0];
                    await room.MarkYourselfReady(null, null);
                    Debug.Log($"[ContextMenu] I am ready.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        [ContextMenu("SetUnReady")]
        public async void SetUnReady()
        {
            try
            {
                if (RoomManager.ListJoinedRooms().Count > 0)
                {
                    var room = RoomManager.ListJoinedRooms()[0];
                    await room.MarkYourselfUnready();
                    Debug.Log($"[ContextMenu] I am not ready.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        [ContextMenu("StartMatchmaking")]
        public async void StartMatchmaking()
        {
            try
            {
                if (RoomManager.ListJoinedRooms().Count > 0)
                {
                    var room = RoomManager.ListJoinedRooms()[0];
                    await room.StartMatchmaking();
                    Debug.Log($"[ContextMenu] MM started.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private CancellationTokenSource _matchmakingCts;

        [ContextMenu("CancelMatchmaking")]
        public async void CancelMatchmaking()
        {
            try
            {
                if (RoomManager.ListJoinedRooms().Count > 0)
                {
                    _matchmakingCts = new CancellationTokenSource();
                    var room = RoomManager.ListJoinedRooms()[0];
                    await room.CancelMatchmaking(_matchmakingCts.Token);
                    Debug.Log($"[ContextMenu] MM canceled.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        [ContextMenu("CancelMatchmakingCancellation")]
        public void CancelMatchmakingCancellation()
        {
            try
            {
                if (_matchmakingCts != null)
                    _matchmakingCts.Cancel();
                else
                    Debug.LogError($"[ContextMenu] Matchmaking is not being cancelled.");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        [ContextMenu("List user status")]
        public void ListUserStatus()
        {
            if (RoomManager.ListJoinedRooms().Count > 0)
                foreach (var user in RoomManager.ListJoinedRooms()[0].State.Users)
                    Debug.Log($"UserInfo: {user}");
        }

        [ContextMenu("List rooms")]
        public void ListRooms()
        {
            foreach (var room in RoomManager.ListAvailableRooms())
                Debug.Log($"{room.State.RoomName} - {room.RoomId}");
        }
    }
}
