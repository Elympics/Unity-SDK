using System;
using System.Collections.Generic;
using Elympics;
using Elympics.Models.Authentication;
using Elympics.Models.Matchmaking;
using UnityEngine;

namespace MatchEvents
{
    public class MatchLifecycleTester : MonoBehaviour
    {
        private void Awake()
        {
            ElympicsLobbyClient.Instance.AuthenticationSucceeded += OnAuthenticationSucceeded;
            ElympicsLobbyClient.Instance.AuthenticationFailed += OnAuthenticationFailed;
            ElympicsLobbyClient.Instance.Matchmaker.MatchmakingStarted += MatchmakerOnMatchmakingStarted;
            ElympicsLobbyClient.Instance.Matchmaker.MatchmakingMatchFound += MatchmakerOnMatchFound;
            ElympicsLobbyClient.Instance.Matchmaker.MatchmakingSucceeded += MatchmakerOnMatchmakingSucceeded;
            ElympicsLobbyClient.Instance.Matchmaker.MatchmakingFailed += MatchmakerOnMatchmakingFailed;
            ElympicsLobbyClient.Instance.Matchmaker.MatchmakingWarning += MatchmakerOnMatchmakingWarning;
            ElympicsLobbyClient.Instance.Matchmaker.MatchmakingCancelledGuid += MatchmakerOnMatchmakingCancelled;
        }

        #region ElympicsLobbyClient

        private static void OnAuthenticationSucceeded(AuthData obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.UserId), obj.UserId },
                { nameof(obj.JwtToken), obj.JwtToken },
                { nameof(obj.AuthType), obj.AuthType },
            });

        private static void OnAuthenticationFailed(string error) => Serializer.PrintCall(
	        new Dictionary<string, object>
	        {
		        { nameof(error), error },
	        });

        #endregion ElympicsLobbyClient

        #region IMatchmakerClient

        private static void MatchmakerOnMatchmakingStarted() => Serializer.PrintCall();

        private static void MatchmakerOnMatchmakingSucceeded(MatchmakingFinishedData obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.MatchId), obj.MatchId },
                { nameof(obj.TcpUdpServerAddress), obj.TcpUdpServerAddress },
                { nameof(obj.WebServerAddress), obj.WebServerAddress },
                { nameof(obj.UserSecret), obj.UserSecret },
                { nameof(obj.MatchedPlayers), obj.MatchedPlayers }
            });

        private static void MatchmakerOnMatchFound(Guid matchId) => Serializer.PrintCall(
	        new Dictionary<string, object>
	        {
		        { nameof(matchId), matchId }
	        });

        private static void MatchmakerOnMatchmakingFailed((string Error, Guid MatchId) obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.Error), obj.Error },
                { nameof(obj.MatchId), obj.MatchId }
            });

        private static void MatchmakerOnMatchmakingWarning((string Warning, Guid MatchId) obj) => Serializer.PrintCall(
	        new Dictionary<string, object>
	        {
		        { nameof(obj.Warning), obj.Warning },
		        { nameof(obj.MatchId), obj.MatchId }
	        });

        private static void MatchmakerOnMatchmakingCancelled(Guid matchId) => Serializer.PrintCall(
	        new Dictionary<string, object>
	        {
		        { nameof(matchId), matchId }
	        });

        #endregion IMatchmakerClient
    }
}
