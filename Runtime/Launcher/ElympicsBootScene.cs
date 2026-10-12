#nullable enable

using System;
using System.Linq;

namespace Elympics
{
    internal static class ElympicsBootScene
    {
        public const string NotSetWarning = nameof(ElympicsConfig) + " has no boot scene, so matches return to an empty scene. "
            + "Set ElympicsConfig > Boot scene to return to your own scene.";

        /// <summary>Null if matches can return to the configured boot scene, otherwise why they return to an empty scene.</summary>
        public static string? FindProblem(ElympicsConfig? config, Func<string, bool> isInBuild)
        {
            var bootScene = config?.BootScene ?? string.Empty;
            if (string.IsNullOrEmpty(bootScene))
                return NotSetWarning;
            if (!isInBuild(bootScene))
                return $"Boot scene {bootScene} is not in Build Settings, so matches return to an empty scene. "
                    + "Set ElympicsConfig > Boot scene to a separate scene in Build Settings, or clear it.";
            if (config!.AvailableGames.Any(game => game != null && game.GameplayScene == bootScene))
                return $"Boot scene {bootScene} is also a gameplay scene, so matches return to an empty scene. "
                    + "Set ElympicsConfig > Boot scene to a separate scene in Build Settings, or clear it.";
            return null;
        }
    }
}
