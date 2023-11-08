using Cysharp.Threading.Tasks;
using UnityEngine;
using Elympics;

public static class ClosestRegionFinder
{
    private static (string Region, float LatencyMs)? CachedClosestRegion;

    public static async UniTask<(string Region, float LatencyMs)> GetClosestRegion()
    {
        if (!CachedClosestRegion.HasValue)
        {
            Debug.Log("Searching for closest region...");
            CachedClosestRegion = await ElympicsCloudPing.ChooseClosestRegion(ElympicsRegions.AllAvailableRegions);
            Debug.Log("Closest region has been cached!");
        }

        return CachedClosestRegion.Value;
    }
}
