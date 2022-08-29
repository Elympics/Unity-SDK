using System.Collections.Generic;
using Elympics;
using UnityEngine;

namespace MatchEvents
{
    public class MatchLifecycleTester : MonoBehaviour
    {
        private void Awake()
        {
            ElympicsLobbyClient.Instance.Authenticated += OnAuthenticated;
            ElympicsLobbyClient.Instance.Matchmaker.LookingForUnfinishedMatchStarted +=
                MatchmakerOnLookingForUnfinishedMatchStarted;
            ElympicsLobbyClient.Instance.Matchmaker.LookingForUnfinishedMatchFinished +=
                MatchmakerOnLookingForUnfinishedMatchFinished;
            ElympicsLobbyClient.Instance.Matchmaker.LookingForUnfinishedMatchError +=
                MatchmakerOnLookingForUnfinishedMatchError;
            ElympicsLobbyClient.Instance.Matchmaker.LookingForUnfinishedMatchCancelled +=
                MatchmakerOnLookingForUnfinishedMatchCancelled;
            ElympicsLobbyClient.Instance.Matchmaker.WaitingForMatchStarted += MatchmakerOnWaitingForMatchStarted;
            ElympicsLobbyClient.Instance.Matchmaker.WaitingForMatchFinished += MatchmakerOnWaitingForMatchFinished;
            ElympicsLobbyClient.Instance.Matchmaker.WaitingForMatchRetried += MatchmakerOnWaitingForMatchRetried;
            ElympicsLobbyClient.Instance.Matchmaker.WaitingForMatchError += MatchmakerOnWaitingForMatchError;
            ElympicsLobbyClient.Instance.Matchmaker.WaitingForMatchCancelled += MatchmakerOnWaitingForMatchCancelled;
            ElympicsLobbyClient.Instance.Matchmaker.WaitingForMatchStateInitializingStartedWithMatchId +=
                MatchmakerOnWaitingForMatchStateInitializingStartedWithMatchId;
            ElympicsLobbyClient.Instance.Matchmaker.WaitingForMatchStateInitializingFinishedWithMatchId +=
                MatchmakerOnWaitingForMatchStateInitializingFinishedWithMatchId;
            ElympicsLobbyClient.Instance.Matchmaker.WaitingForMatchStateInitializingRetriedWithMatchId +=
                MatchmakerOnWaitingForMatchStateInitializingRetriedWithMatchId;
            ElympicsLobbyClient.Instance.Matchmaker.WaitingForMatchStateInitializingError +=
                MatchmakerOnWaitingForMatchStateInitializingError;
            ElympicsLobbyClient.Instance.Matchmaker.WaitingForMatchStateInitializingCancelledWithMatchId +=
                MatchmakerOnWaitingForMatchStateInitializingCancelledWithMatchId;
            ElympicsLobbyClient.Instance.Matchmaker.WaitingForMatchStateRunningStartedWithMatchId +=
                MatchmakerOnWaitingForMatchStateRunningStartedWithMatchId;
            ElympicsLobbyClient.Instance.Matchmaker.WaitingForMatchStateRunningFinished +=
                MatchmakerOnWaitingForMatchStateRunningFinished;
            ElympicsLobbyClient.Instance.Matchmaker.WaitingForMatchStateRunningRetriedWithMatchId +=
                MatchmakerOnWaitingForMatchStateRunningRetriedWithMatchId;
            ElympicsLobbyClient.Instance.Matchmaker.WaitingForMatchStateRunningError +=
                MatchmakerOnWaitingForMatchStateRunningError;
            ElympicsLobbyClient.Instance.Matchmaker.WaitingForMatchStateRunningCancelledWithMatchId +=
                MatchmakerOnWaitingForMatchStateRunningCancelledWithMatchId;
            ElympicsLobbyClient.Instance.Matchmaker.MatchmakingStarted += MatchmakerOnMatchmakingStarted;
            ElympicsLobbyClient.Instance.Matchmaker.MatchmakingFinished += MatchmakerOnMatchmakingFinished;
            ElympicsLobbyClient.Instance.Matchmaker.MatchmakingError += MatchmakerOnMatchmakingError;
            ElympicsLobbyClient.Instance.Matchmaker.MatchmakingCancelled += MatchmakerOnMatchmakingCancelled;
        }

        #region ElympicsLobbyClient

        private static void OnAuthenticated(bool success, string userid, string jwttoken, string error) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(success), success },
                { nameof(userid), userid },
                { nameof(jwttoken), jwttoken },
                { nameof(error), error }
            });

        #endregion ElympicsLobbyClient

        #region IMatchmakerClient

        private static void MatchmakerOnLookingForUnfinishedMatchStarted((string GameId, string GameVersion) obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.GameId), obj.GameId },
                { nameof(obj.GameVersion), obj.GameVersion }
            });

        private static void MatchmakerOnLookingForUnfinishedMatchFinished((string GameId, string GameVersion, string MatchId) obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.GameId), obj.GameId },
                { nameof(obj.GameVersion), obj.GameVersion },
                { nameof(obj.MatchId), obj.MatchId }
            });

        private static void MatchmakerOnLookingForUnfinishedMatchError((string GameId, string GameVersion, string Error) obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.GameId), obj.GameId },
                { nameof(obj.GameVersion), obj.GameVersion },
                { nameof(obj.Error), obj.Error }
            });

        private static void MatchmakerOnLookingForUnfinishedMatchCancelled((string GameId, string GameVersion) obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.GameId), obj.GameId },
                { nameof(obj.GameVersion), obj.GameVersion }
            });

        private static void MatchmakerOnWaitingForMatchStarted((string GameId, string GameVersion) obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.GameId), obj.GameId },
                { nameof(obj.GameVersion), obj.GameVersion }
            });

        private static void MatchmakerOnWaitingForMatchFinished((string GameId, string GameVersion, string MatchId) obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.GameId), obj.GameId },
                { nameof(obj.GameVersion), obj.GameVersion },
                { nameof(obj.MatchId), obj.MatchId }
            });

        private static void MatchmakerOnWaitingForMatchRetried((string GameId, string GameVersion) obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.GameId), obj.GameId },
                { nameof(obj.GameVersion), obj.GameVersion }
            });

        private static void MatchmakerOnWaitingForMatchError((string GameId, string GameVersion, string Error) obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.GameId), obj.GameId },
                { nameof(obj.GameVersion), obj.GameVersion },
                { nameof(obj.Error), obj.Error }
            });

        private static void MatchmakerOnWaitingForMatchCancelled((string GameId, string GameVersion) obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.GameId), obj.GameId },
                { nameof(obj.GameVersion), obj.GameVersion }
            });

        private static void MatchmakerOnWaitingForMatchStateInitializingStartedWithMatchId(string obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj), obj }
            });

        private static void MatchmakerOnWaitingForMatchStateInitializingFinishedWithMatchId(string obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj), obj }
            });

        private static void MatchmakerOnWaitingForMatchStateInitializingRetriedWithMatchId(string obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj), obj }
            });

        private static void MatchmakerOnWaitingForMatchStateInitializingError((string MatchId, string Error) obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.MatchId), obj.MatchId },
                { nameof(obj.Error), obj.Error }
            });

        private static void MatchmakerOnWaitingForMatchStateInitializingCancelledWithMatchId(string obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj), obj }
            });

        private static void MatchmakerOnWaitingForMatchStateRunningStartedWithMatchId(string obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj), obj }
            });

        private static void MatchmakerOnWaitingForMatchStateRunningFinished((string MatchId, string TcpUdpServerAddress, string WebServerAddress, List<string> MatchedPlayers) obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.MatchId), obj.MatchId },
                { nameof(obj.TcpUdpServerAddress), obj.TcpUdpServerAddress },
                { nameof(obj.WebServerAddress), obj.WebServerAddress },
                { nameof(obj.MatchedPlayers), obj.MatchedPlayers }
            });

        private static void MatchmakerOnWaitingForMatchStateRunningRetriedWithMatchId(string obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj), obj }
            });

        private static void MatchmakerOnWaitingForMatchStateRunningError((string MatchId, string Error) obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.MatchId), obj.MatchId },
                { nameof(obj.Error), obj.Error }
            });

        private static void MatchmakerOnWaitingForMatchStateRunningCancelledWithMatchId(string obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj), obj }
            });

        private static void MatchmakerOnMatchmakingStarted() => Serializer.PrintCall();

        private static void MatchmakerOnMatchmakingFinished((string MatchId, string TcpUdpServerAddress, string WebServerAddress, string UserSecret, List<string> MatchedPlayers) obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.MatchId), obj.MatchId },
                { nameof(obj.TcpUdpServerAddress), obj.TcpUdpServerAddress },
                { nameof(obj.WebServerAddress), obj.WebServerAddress },
                { nameof(obj.UserSecret), obj.UserSecret },
                { nameof(obj.MatchedPlayers), obj.MatchedPlayers }
            });

        private static void MatchmakerOnMatchmakingError(string obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj), obj }
            });

        private static void MatchmakerOnMatchmakingCancelled() => Serializer.PrintCall();

        #endregion IMatchmakerClient
    }
}
