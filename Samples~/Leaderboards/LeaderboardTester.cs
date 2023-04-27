using Elympics;
using System;
using Elympics.Models.Authentication;
using UnityEngine;

public class LeaderboardTester : MonoBehaviour
{
    [SerializeField] private int pageSize = 5;
    [SerializeField] private string queue;
    [SerializeField] private LeaderboardGameVersion gameVersion = LeaderboardGameVersion.All;
    [SerializeField] private LeaderboardTimeScopeType timeScope = LeaderboardTimeScopeType.Month;
    [SerializeField] private string customTimeScopeFrom = "2023-04-21T12:00:00+02:00";
    [SerializeField] private string customTimeScopeTo = "2023-04-22T12:00:00+02:00";

    private LeaderboardClient _leaderboardClient;

    private void Start()
    {
        CreateLeaderboardClient();

        ElympicsLobbyClient.Instance.AuthenticationSucceeded += HandleAuthenticated;
    }

    private void HandleAuthenticated(AuthData result)
    {
        Debug.Log("User authenticated - can start using LeaderboardClient");
        FetchFirst();
    }

    private void CreateLeaderboardClient()
    {
        var timeScopeObject = timeScope != LeaderboardTimeScopeType.Custom
	        ? new LeaderboardTimeScope(timeScope)
	        : new LeaderboardTimeScope(DateTimeOffset.Parse(customTimeScopeFrom), DateTimeOffset.Parse(customTimeScopeTo));
        _leaderboardClient = new LeaderboardClient(pageSize, timeScopeObject, queue, gameVersion);
    }
    private void FetchFirst() => _leaderboardClient.FetchFirstPage(DisplayEntries, CustomFailHandler);
    private void FetchUserPage() => _leaderboardClient.FetchPageWithUser(DisplayEntries);
    private void FetchNext() => _leaderboardClient.FetchNextPage(DisplayEntries);
    private void FetchPrevious() => _leaderboardClient.FetchPreviousPage(DisplayEntries);
    private void FetchCurrent() => _leaderboardClient.FetchRefreshedCurrentPage(DisplayEntries);

    private void DisplayEntries(LeaderboardFetchResult result)
    {
        var totalPages = (int)Math.Ceiling(result.TotalRecords / (float)pageSize);
        Debug.Log($"Fetched leaderboards: {result.Entries.Count} entries, page {result.PageNumber} of {totalPages}");
        foreach (var entry in result.Entries)
            Debug.Log($"{entry.Position}. Score: {entry.Score} User: {entry.UserId} When: {entry.ScoredAt.LocalDateTime}");
        FetchNext();
    }

    private static void CustomFailHandler(LeaderboardFetchError fetchError)
    {
        Debug.LogError(fetchError);

        if (fetchError == LeaderboardFetchError.NoRecords)
            Debug.Log("You need to generate any match results to be able to view them in the leaderboards");
    }
}
