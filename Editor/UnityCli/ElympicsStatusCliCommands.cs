#nullable enable

using System.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Elympics.Editor.UnityCli
{
    public static class ElympicsStatusCliCommands
    {
        [CliCommand("elympics_status",
            "Report the Elympics SDK version, login state (no user name or tokens), ElympicsConfig, the active and configured games, endpoints and Editor state. "
            + "Works offline, logged out and without a config.",
            Tags = new[] { "elympics", "read" })]
        public static object GetStatus() => CliRun.Run(() =>
        {
            var sdkPackage = PackageInfo.FindForAssembly(typeof(ElympicsConfig).Assembly);
            var pipelinePackage = PackageInfo.FindForAssembly(typeof(CliCommandAttribute).Assembly);
            var config = ElympicsConfig.Load();
            var activeGame = config?.GetCurrentGameConfig();

            return new
            {
                status = "ok",
                sdk = new
                {
                    version = sdkPackage?.version ?? ElympicsConfig.SdkVersion,
                    source = sdkPackage?.source.ToString().ToLowerInvariant(),
                    sdkPackage?.resolvedPath,
                },
                pipelineVersion = pipelinePackage?.version,
                UnityEngine.Application.unityVersion,
                login = new { loggedIn = ElympicsConfig.IsLogin },
                config = new
                {
                    present = config != null,
                    assetPath = config != null ? AssetDatabase.GetAssetPath(config) : null,
                    bootScene = config != null && !string.IsNullOrEmpty(config.BootScene) ? config.BootScene : null,
                    gameCount = config != null ? config.AvailableGames.Count(game => game != null) : 0,
                },
                activeGame = activeGame == null
                    ? null
                    : new
                    {
                        gameId = activeGame.GameId,
                        gameName = activeGame.GameName,
                        gameVersion = activeGame.GameVersion,
                        maxPlayers = activeGame.MaxPlayers,
                        gameplayScene = activeGame.GameplayScene,
                        ticksPerSecond = activeGame.TicksPerSecond,
                        assetPath = AssetDatabase.GetAssetPath(activeGame),
                    },
                games = config == null
                    ? new object[0]
                    : config.AvailableGames.Select((game, index) => game == null
                        ? (object)new { index, missing = true }
                        : new { index, gameId = game.GameId, gameName = game.GameName, gameVersion = game.GameVersion, assetPath = AssetDatabase.GetAssetPath(game) }).ToArray(),
                endpoints = config == null ? null : new { api = config.ElympicsApiEndpoint, gameServers = config.ElympicsGameServersEndpoint },
                editor = new
                {
                    EditorApplication.isPlaying,
                    EditorApplication.isCompiling,
                    compilationFailed = EditorUtility.scriptCompilationFailed,
                    activeBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                },
            };
        });
    }
}
