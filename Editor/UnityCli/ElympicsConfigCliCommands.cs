using System;
using System.IO;
using System.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Elympics.Editor.UnityCli
{
    public static class ElympicsConfigCliCommands
    {
        [CliCommand("elympics_config_select_game",
            "Make a game config the active one (ElympicsConfig's first game) and save ElympicsConfig. "
            + "A game config asset that isn't in ElympicsConfig yet is registered first (found by --game_id, or given by --config_asset).",
            Tags = new[] { "elympics", "write" })]
        public static object SelectGame(
            [CliArg("game_id", "Elympics game ID of the game config to activate.")] string gameId = "",
            [CliArg("index", "Index of the game in ElympicsConfig (see elympics_status), instead of --game_id.")] int index = -1,
            [CliArg("config_asset", "Asset path of an ElympicsGameConfig to register and activate, e.g. after elympics_import_sample.")] string configAsset = "",
            [CliArg("create_if_missing", "Create ElympicsConfig (Assets/Resources/Elympics) if the project has none.")] bool createIfMissing = false) => CliRun.Sync(() =>
        {
            var given = new[] { !string.IsNullOrWhiteSpace(gameId), index >= 0, !string.IsNullOrWhiteSpace(configAsset) }.Count(isGiven => isGiven);
            if (given != 1)
                throw ElympicsCliErrors.InvalidArgumentException("Pass exactly one of --game_id, --index and --config_asset.",
                    "pass e.g. --game_id <id from elympics_status> or --config_asset Assets/.../GameConfig.asset.");

            var assetGameConfig = !string.IsNullOrWhiteSpace(configAsset) ? LoadGameConfigAsset(configAsset) : null;
            var config = CliGames.RequireConfig(createIfMissing);
            var previousGameId = config.GetCurrentGameConfig()?.GameId;
            var gameConfig = assetGameConfig ?? (index >= 0 ? GetByIndex(config, index) : FindByGameId(config, gameId.Trim()));

            Undo.RecordObject(config, "Select Elympics game");
            var registered = false;
            if (!config.availableGames.Contains(gameConfig))
            {
                config.availableGames.Add(gameConfig);
                registered = true;
            }
            config.SwitchGame(config.availableGames.IndexOf(gameConfig));
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            return new
            {
                status = "selected",
                gameId = gameConfig.GameId,
                gameName = gameConfig.GameName,
                assetPath = AssetDatabase.GetAssetPath(gameConfig),
                previousGameId,
                registeredAsset = registered,
            };
        });

        private static ElympicsGameConfig LoadGameConfigAsset(string assetPath) =>
            AssetDatabase.LoadAssetAtPath<ElympicsGameConfig>(assetPath.Trim())
            ?? throw new ElympicsCliException(ElympicsCliErrors.GameNotFound, $"No ElympicsGameConfig asset at {assetPath}.",
                "pass the asset path of an ElympicsGameConfig (find_assets --type ElympicsGameConfig lists them), or select the game by --game_id.");

        private static ElympicsGameConfig GetByIndex(ElympicsConfig config, int index)
        {
            if (index >= config.availableGames.Count || config.availableGames[index] == null)
                throw ElympicsCliErrors.InvalidArgumentException($"ElympicsConfig has no game at index {index}.",
                    "use an index listed under \"games\" by elympics_status.");
            return config.availableGames[index];
        }

        private static ElympicsGameConfig FindByGameId(ElympicsConfig config, string gameId)
        {
            var listed = config.AvailableGames.FirstOrDefault(game => game != null && game.GameId == gameId);
            if (listed != null)
                return listed;
            var asset = AssetDatabase.FindAssets($"t:{nameof(ElympicsGameConfig)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ElympicsGameConfig>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(game => game != null && game.GameId == gameId);
            return asset ?? throw new ElympicsCliException(ElympicsCliErrors.GameNotFound, $"No ElympicsGameConfig asset in the project has the game ID {gameId}.",
                "import a sample (elympics_import_sample), create a game with elympics_game_create --create_config true, or pass --config_asset.");
        }

        [CliCommand("elympics_config_set_version", "Set the game version of a game config (the active one by default) and save it.", Tags = new[] { "elympics", "write" })]
        public static object SetVersion(
            [CliArg("version", "New game version: 1-63 Latin letters, digits, dashes and dots.", Required = true)] string version,
            [CliArg("game_id", "Elympics game ID of a game config in ElympicsConfig. Defaults to the active game.")] string gameId = "") => CliRun.Sync(() =>
        {
            version = CliArgs.ValidateVersion(version, "version");
            var gameConfig = CliGames.ResolveGame(gameId);
            var oldVersion = gameConfig.GameVersion;
            Undo.RecordObject(gameConfig, "Set Elympics game version");
            gameConfig.UpdateGameVersion(version);
            AssetDatabase.SaveAssets();
            return new { status = "updated", gameId = gameConfig.GameId, oldVersion, newVersion = version };
        });

        [CliCommand("elympics_import_sample",
            "Import a sample of the Elympics package into Assets/Samples. Refuses a sample that is already imported unless --force true. "
            + "Doesn't change ElympicsConfig: select an imported game config with elympics_config_select_game --config_asset.",
            Tags = new[] { "elympics", "write" })]
        public static object ImportSample(
            [CliArg("sample", "Sample display name or directory name, case-insensitive (e.g. \"Button Clicker [Game]\" or ButtonClicker).", Required = true)] string sample,
            [CliArg("force", "Import again over an already imported sample.")] bool force = false) => CliRun.Sync(() =>
        {
            sample = CliArgs.Require(sample, "sample");
            var package = PackageInfo.FindForAssembly(typeof(ElympicsConfig).Assembly);
            var samples = Sample.FindByPackage(package.name, package.version).ToList();
            var match = samples.FirstOrDefault(candidate => candidate.displayName.Equals(sample, StringComparison.OrdinalIgnoreCase)
                || Path.GetFileName(candidate.resolvedPath.TrimEnd('/', '\\')).Equals(sample, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(match.displayName))
                throw new ElympicsCliException(ElympicsCliErrors.SampleNotFound, $"The Elympics package has no sample \"{sample}\".",
                    $"use one of: {string.Join(", ", samples.Select(candidate => $"\"{candidate.displayName}\""))}.");
            if (match.isImported && !force)
                throw new ElympicsCliException(ElympicsCliErrors.SampleAlreadyImported, $"The sample \"{match.displayName}\" is already imported at {ToAssetPath(match.importPath)}.",
                    "use the imported copy, or repeat with --force true to import it again over it.");

            if (!match.Import(force ? Sample.ImportOptions.OverridePreviousImports : Sample.ImportOptions.None))
                throw new ElympicsCliException(ElympicsCliErrors.SampleImportFailed, $"Importing the sample \"{match.displayName}\" failed.",
                    "check the Console (console --level error) for the reason, fix it and retry.");

            var importPath = ToAssetPath(match.importPath);
            AssetDatabase.Refresh();
            var files = Directory.Exists(match.importPath)
                ? Directory.GetFiles(match.importPath, "*", SearchOption.AllDirectories).Where(file => !file.EndsWith(".meta", StringComparison.Ordinal))
                    .Select(ToAssetPath).ToList()
                : new System.Collections.Generic.List<string>();
            return new
            {
                status = "imported",
                sample = match.displayName,
                importPath,
                alreadyImported = false,
                fileCount = files.Count,
                files = files.Take(50).ToArray(),
                gameConfigs = AssetDatabase.FindAssets($"t:{nameof(ElympicsGameConfig)}", new[] { importPath }).Select(AssetDatabase.GUIDToAssetPath).ToArray(),
            };
        });

        private static string ToAssetPath(string path)
        {
            var fullPath = Path.GetFullPath(path).Replace('\\', '/');
            var projectPath = Path.GetFullPath(".").Replace('\\', '/').TrimEnd('/') + "/";
            return fullPath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase) ? fullPath[projectPath.Length..] : fullPath;
        }
    }
}
