using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Elympics
{
    /// <summary>
    /// Create leaderboard client with desired parameters and use its fetch methods with custom callbacks
    /// </summary>
    public class LeaderboardClient
    {
        private const string LeaderboardsUrl = "https://api.elympics.cc/v2/leaderboardservice/leaderboard";
        private const string LeaderboardsUserCenteredUrl = "https://api.elympics.cc/v2/leaderboardservice/leaderboard/user-centred";

        private readonly Dictionary<string, string> _queryValues;

        private bool _isBusy;
        private int _currentPage = 1;

        /// <param name="pageSize">Must be in range 1 - 100 (inclusive)</param>
        /// <param name="queueName">No filtering by queue if null provided</param>
        public LeaderboardClient(int pageSize, LeaderboardTimeScope timeScope, string queueName = null, LeaderboardGameVersion gameVersion = LeaderboardGameVersion.All)
        {
            if (pageSize is < 1 or > 100)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Must be in range 1 - 100 (inclusive)");

            if (timeScope == null)
                throw new ArgumentNullException(nameof(timeScope));

            if (!Enum.IsDefined(typeof(LeaderboardGameVersion), gameVersion))
                throw new ArgumentOutOfRangeException(nameof(gameVersion));

            var gameConfig = ElympicsConfig.Load().GetCurrentGameConfig();
            if (gameConfig == null)
                throw new ElympicsException("Provide ElympicsGameConfig before proceeding");

            _queryValues = new Dictionary<string, string>
            {
                { "PageNumber", _currentPage.ToString() },
                { "PageSize", pageSize.ToString() },
                { "GameId", gameConfig.GameId },
                { "GameVersion", gameVersion == LeaderboardGameVersion.All ? null : gameConfig.GameVersion },
                { "QueueName", queueName },
                { "TimeScope", timeScope.LeaderboardTimeScopeType.ToString() },
            };

            if (timeScope.LeaderboardTimeScopeType == LeaderboardTimeScopeType.Custom)
            {
                _queryValues.Add("DateFrom", timeScope.DateFrom.ToString("o"));
                _queryValues.Add("DateTo", timeScope.DateTo.ToString("o"));
            }
        }


        public void FetchFirstPage(Action<LeaderboardFetchResult> onSuccess, Action<LeaderboardFetchError> onFailure = null) => SendLeaderboardRequest(LeaderboardsUrl, 1, onSuccess, onFailure);
        public void FetchPageWithUser(Action<LeaderboardFetchResult> onSuccess, Action<LeaderboardFetchError> onFailure = null) => SendLeaderboardRequest(LeaderboardsUserCenteredUrl, _currentPage, onSuccess, onFailure);
        public void FetchNextPage(Action<LeaderboardFetchResult> onSuccess, Action<LeaderboardFetchError> onFailure = null) => SendLeaderboardRequest(LeaderboardsUrl, _currentPage + 1, onSuccess, onFailure);
        public void FetchPreviousPage(Action<LeaderboardFetchResult> onSuccess, Action<LeaderboardFetchError> onFailure = null) => SendLeaderboardRequest(LeaderboardsUrl, _currentPage - 1, onSuccess, onFailure);
        public void FetchRefreshedCurrentPage(Action<LeaderboardFetchResult> onSuccess, Action<LeaderboardFetchError> onFailure = null) => SendLeaderboardRequest(LeaderboardsUrl, _currentPage, onSuccess, onFailure);

        private void SendLeaderboardRequest(string url, int pageNumber, Action<LeaderboardFetchResult> onSuccess, Action<LeaderboardFetchError> onFailure)
        {
            onFailure ??= DefaultFailure;

            if (_isBusy)
                onFailure(LeaderboardFetchError.RequestAlreadyInProgress);
            else if (!ElympicsLobbyClient.Instance.IsAuthenticated)
                onFailure(LeaderboardFetchError.NotAuthenticated);
            else if (pageNumber < 1)
                onFailure(LeaderboardFetchError.PageLessThanOne);
            else
            {
                _isBusy = true;
                _queryValues["PageNumber"] = pageNumber.ToString();
                var authorization = ElympicsLobbyClient.Instance.AuthData.BearerAuthorization;
                ElympicsWebClient.SendGetRequest(url, _queryValues, authorization, HandleCallback(onSuccess, onFailure));
            }
        }


        private Action<Result<LeaderboardResponse, Exception>> HandleCallback(Action<LeaderboardFetchResult> onSuccess, Action<LeaderboardFetchError> onFailure)
        {
            return result =>
            {
                _isBusy = false;

                if (result.IsSuccess)
                {
                    if (result.Value?.data?.Any() is true)
                    {
                        _currentPage = result.Value.pageNumber;
                        onSuccess(new LeaderboardFetchResult(result.Value));
                    }
                    else if (result.Value?.pageNumber == 1)
                        onFailure(LeaderboardFetchError.NoRecords);
                    else
                        onFailure(LeaderboardFetchError.PageGreaterThanMax);
                }
                else
                {
                    var errorMessage = result.Error.ToString();

                    if (errorMessage.Contains("There is no score of this user in the leaderboard."))
                        onFailure(LeaderboardFetchError.NoScoresForUser);
                    else if (errorMessage.Contains("401"))
                        onFailure(LeaderboardFetchError.NotAuthenticated);
                    else
                    {
                        onFailure(LeaderboardFetchError.UnknownError);
                        Debug.LogError(errorMessage);
                    }
                }
            };
        }

        private static void DefaultFailure(LeaderboardFetchError error) => Debug.LogError(error);
    }

    public enum LeaderboardGameVersion
    {
        All,
        Current,
    }
}
