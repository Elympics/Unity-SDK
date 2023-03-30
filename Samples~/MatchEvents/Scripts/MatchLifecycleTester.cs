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
            ElympicsLobbyClient.Instance.AuthenticatedGuid += OnAuthenticated;
            ElympicsLobbyClient.Instance.AuthenticatedWithType += OnAuthenticatedWithType;
            ElympicsLobbyClient.Instance.Matchmaker.MatchmakingStarted += MatchmakerOnMatchmakingStarted;
            ElympicsLobbyClient.Instance.Matchmaker.MatchmakingMatchFound += MatchmakerOnMatchFound;
            ElympicsLobbyClient.Instance.Matchmaker.MatchmakingSucceeded += MatchmakerOnMatchmakingSucceeded;
            ElympicsLobbyClient.Instance.Matchmaker.MatchmakingFailed += MatchmakerOnMatchmakingFailed;
            ElympicsLobbyClient.Instance.Matchmaker.MatchmakingWarning += MatchmakerOnMatchmakingWarning;
            ElympicsLobbyClient.Instance.Matchmaker.MatchmakingCancelledGuid += MatchmakerOnMatchmakingCancelled;
        }

        #region ElympicsLobbyClient

        private static void OnAuthenticated(Result<AuthenticationData, string> obj) => Serializer.PrintCall(
            new Dictionary<string, object>
            {
                { nameof(obj.IsSuccess), obj.IsSuccess },
                { nameof(obj.Value.UserId), obj.Value?.UserId },
                { nameof(obj.Value.JwtToken), obj.Value?.JwtToken },
                { nameof(obj.Error), obj.Error }
            });

        private static void OnAuthenticatedWithType(AuthType type, Result<AuthenticationData, string> obj) => Serializer.PrintCall(
	        new Dictionary<string, object>
	        {
		        { nameof(type), type },
		        { nameof(obj.IsSuccess), obj.IsSuccess },
		        { nameof(obj.Value.UserId), obj.Value?.UserId },
		        { nameof(obj.Value.JwtToken), obj.Value?.JwtToken },
		        { nameof(obj.Error), obj.Error }
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
