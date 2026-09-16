using System.Collections.Generic;
using Elympics;
using Elympics.ElympicsSystems;
using UnityEngine;

namespace MatchEvents
{
    public class MatchLifecycleTester : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Assert(ElympicsLobbyClient.Instance != null);

            ElympicsLobbyClient.Instance.ElympicsConnectionEstablished += OnElympicsConnectionEstablished;
            ElympicsLobbyClient.Instance.ElympicsConnectionLost += OnElympicsConnectionLost;

            ElympicsLobbyClient.Instance.RoomsManager.RoomListUpdated += RoomsOnRoomListUpdated;
            ElympicsLobbyClient.Instance.RoomsManager.JoinedRoomUpdated += RoomsOnJoinedRoomUpdated;
            ElympicsLobbyClient.Instance.RoomsManager.JoinedRoom += RoomsOnJoinedRoom;
            ElympicsLobbyClient.Instance.RoomsManager.LeftRoom += RoomsOnLeftRoom;
            ElympicsLobbyClient.Instance.RoomsManager.UserJoined += RoomsOnUserJoined;
            ElympicsLobbyClient.Instance.RoomsManager.UserLeft += RoomsOnUserLeft;
            ElympicsLobbyClient.Instance.RoomsManager.UserCountChanged += RoomsOnUserCountChanged;
            ElympicsLobbyClient.Instance.RoomsManager.HostChanged += RoomsOnHostChanged;
            ElympicsLobbyClient.Instance.RoomsManager.UserReadinessChanged += RoomsOnUserReadinessChanged;
            ElympicsLobbyClient.Instance.RoomsManager.UserChangedTeam += RoomsOnUserChangedTeam;
            ElympicsLobbyClient.Instance.RoomsManager.CustomRoomDataChanged += RoomsOnCustomRoomDataChanged;
            ElympicsLobbyClient.Instance.RoomsManager.CustomPlayerDataChanged += RoomsOnCustomPlayerDataChanged;
            ElympicsLobbyClient.Instance.RoomsManager.RoomPublicnessChanged += RoomsOnRoomPublicnessChanged;
            ElympicsLobbyClient.Instance.RoomsManager.RoomNameChanged += RoomsOnRoomNameChanged;
            ElympicsLobbyClient.Instance.RoomsManager.RoomBetAmountChanged += RoomsOnRoomBetAmountChanged;
            ElympicsLobbyClient.Instance.RoomsManager.MatchmakingDataChanged += RoomsOnMatchmakingDataChanged;
            ElympicsLobbyClient.Instance.RoomsManager.MatchmakingStarted += RoomsOnMatchmakingStarted;
            ElympicsLobbyClient.Instance.RoomsManager.MatchmakingEnded += RoomsOnMatchmakingEnded;
            ElympicsLobbyClient.Instance.RoomsManager.MatchDataReceived += RoomsOnMatchDataReceived;
            ElympicsLobbyClient.Instance.RoomsManager.CustomMatchmakingDataChanged += RoomsOnCustomMatchmakingDataChanged;
        }

        #region ElympicsLobbyClient

        private static void OnElympicsConnectionEstablished(ElympicsConnectionData obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.AuthData), obj.AuthData },
                { nameof(obj.AutoReconnected), obj.AutoReconnected },
            });

        private static void OnElympicsConnectionLost(ElympicsConnectionLostData obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.DisconnectionData), obj.DisconnectionData },
            });

        #endregion ElympicsLobbyClient

        #region IRoomsManager

        private static void RoomsOnRoomListUpdated(RoomListUpdatedArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomIds), obj.RoomIds },
            });

        private static void RoomsOnJoinedRoomUpdated(JoinedRoomUpdatedArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomId), obj.RoomId },
            });

        private static void RoomsOnJoinedRoom(JoinedRoomArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomId), obj.RoomId },
            });

        private static void RoomsOnLeftRoom(LeftRoomArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomId), obj.RoomId },
                { nameof(obj.Reason), obj.Reason },
            });

        private static void RoomsOnUserJoined(UserJoinedArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomId), obj.RoomId },
                { nameof(obj.User), obj.User },
            });

        private static void RoomsOnUserLeft(UserLeftArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomId), obj.RoomId },
                { nameof(obj.User), obj.User },
            });

        private static void RoomsOnUserCountChanged(UserCountChangedArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomId), obj.RoomId },
                { nameof(obj.UserCount), obj.UserCount },
            });

        private static void RoomsOnHostChanged(HostChangedArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomId), obj.RoomId },
                { nameof(obj.UserId), obj.UserId },
            });

        private static void RoomsOnUserReadinessChanged(UserReadinessChangedArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomId), obj.RoomId },
                { nameof(obj.UserId), obj.UserId },
                { nameof(obj.IsReady), obj.IsReady },
            });

        private static void RoomsOnUserChangedTeam(UserChangedTeamArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomId), obj.RoomId },
                { nameof(obj.UserId), obj.UserId },
                { nameof(obj.TeamIndex), obj.TeamIndex },
            });

        private static void RoomsOnCustomRoomDataChanged(CustomRoomDataChangedArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomId), obj.RoomId },
                { nameof(obj.Key), obj.Key },
                { nameof(obj.Value), obj.Value },
            });

        private static void RoomsOnCustomPlayerDataChanged(CustomPlayerDataChangedArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.UserId), obj.UserId },
                { nameof(obj.Key), obj.Key },
                { nameof(obj.Value), obj.Value },
            });

        private static void RoomsOnRoomPublicnessChanged(RoomPublicnessChangedArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomId), obj.RoomId },
                { nameof(obj.IsPrivate), obj.IsPrivate },
            });

        private static void RoomsOnRoomNameChanged(RoomNameChangedArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomId), obj.RoomId },
                { nameof(obj.RoomName), obj.RoomName },
            });

        private static void RoomsOnRoomBetAmountChanged(RoomBetAmountChangedArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomId), obj.RoomId },
                { nameof(obj.NewBetValue), obj.NewBetValue },
            });

        private static void RoomsOnMatchmakingDataChanged(MatchmakingDataChangedArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomId), obj.RoomId },
            });

        private static void RoomsOnMatchmakingStarted(MatchmakingStartedArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomId), obj.RoomId },
            });

        private static void RoomsOnMatchmakingEnded(MatchmakingEndedArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomId), obj.RoomId },
            });

        private static void RoomsOnMatchDataReceived(MatchDataReceivedArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomId), obj.RoomId },
                { nameof(obj.MatchId), obj.MatchId },
                { nameof(obj.QueueName), obj.QueueName },
                { nameof(obj.MatchData), obj.MatchData },
            });

        private static void RoomsOnCustomMatchmakingDataChanged(CustomMatchmakingDataChangedArgs obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.RoomId), obj.RoomId },
                { nameof(obj.Key), obj.Key },
                { nameof(obj.Value), obj.Value },
            });


        #endregion IRoomsManager
    }
}
