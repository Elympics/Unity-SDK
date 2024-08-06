using System;
using System.Globalization;
using System.Linq;
#nullable enable
namespace Elympics
{
    internal static class ElympicsLogTemplates
    {
        internal static void LogJoiningMatchmaker(Guid userId, float[]? matchmakerData, byte[]? gameEngineData, string? queueName, string? regionName, bool loadGameplaySceneOnFinished)
        {
            var serializedMmData = matchmakerData != null
                ? "[" + string.Join(", ", matchmakerData.Select(x => x.ToString(CultureInfo.InvariantCulture))) + "]"
                : "null";
            var serializedGeData = gameEngineData != null
                ? Convert.ToBase64String(gameEngineData)
                : "null";
            ElympicsLogger.Log($"Starting matchmaking process for user: {userId}, region: {regionName}, queue: {queueName}\n"
                + $"Supplied matchmaker data: {serializedMmData}\n"
                + $"Supplied game engine data: {serializedGeData}");
            if (loadGameplaySceneOnFinished)
                ElympicsLogger.Log("Gameplay scene will be loaded after matchmaking succeeds.");
        }
    }

}
