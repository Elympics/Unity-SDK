#if UNITY_2020_2_OR_NEWER
using System.IO;
using System.Linq;
using System;
using UnityEditor;

namespace Elympics
{
    public static class ReplayFileManager
    {
        private const string ElympicsFileFormatName = "ELX";
        private const string ServerIndicator = "S";
        private const string ServerFileReplayExtension = "elxs";
        private const string DefaultOutputFilename = "replay";

        // bump every format change
        private static readonly int ElympicsFileFormatVersion = 2;

        internal static void SaveServerReplay(TickListDisplayer tickListDisplayer, TickDataDisplayer tickDataDisplayer)
        {
            var path = EditorUtility.SaveFilePanel("Save server replay", string.Empty, Path.ChangeExtension(DefaultOutputFilename, ServerFileReplayExtension), ServerFileReplayExtension);
            if (string.IsNullOrEmpty(path))
                return;

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs);
            SerializeServerData(bw, tickListDisplayer, tickDataDisplayer);
        }

        internal static bool LoadServerReplay(TickListDisplayer tickListDisplayer, TickDataDisplayer tickDataDisplayer)
        {
            var path = EditorUtility.OpenFilePanel("Load server replay", string.Empty, ServerFileReplayExtension);
            if (string.IsNullOrEmpty(path))
                return false;

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            if (!DeserializeServerData(br, tickListDisplayer, tickDataDisplayer))
            {
                EditorUtility.DisplayDialog("Wrong replay file", "Could not read provided file or it was uncompatible, outdated or had wrong format. " +
                    "File is considered uncompatible, when gameID, game name, " +
                    "game version or number of players is different than in the file provided.", "OK");
                return false;
            }

            return true;
        }

        private static void SerializeServerData(BinaryWriter bw, TickListDisplayer tickListDisplayer, TickDataDisplayer tickDataDisplayer)
        {
            // file info
            bw.Write(ElympicsFileFormatName + ServerIndicator);
            bw.Write(ElympicsFileFormatVersion);
            bw.Write(DateTime.Now.ToBinary());

            // game info
            var config = ElympicsConfig.LoadCurrentElympicsGameConfig();
            bw.Write(config.GameName);
            bw.Write(config.GameId);
            bw.Write(config.GameVersion);
            bw.Write(config.Players);
            var elympicsVersion = ElympicsVersionRetriever.GetVersionFromAssembly();
            bw.Write(elympicsVersion.Major);
            bw.Write(elympicsVersion.Minor);
            bw.Write(elympicsVersion.Build);

            // time threshold
            bw.Write(ServerAnalyzerUtils.ExpectedTime);

            // input view
            bw.Write(tickDataDisplayer.IsBots.Length);
            foreach (var isBot in tickDataDisplayer.IsBots)
            {
                bw.Write(isBot);
            }

            // snapshots
            bw.Write(tickListDisplayer.AllTicksData.Count);
            foreach (var entry in tickListDisplayer.AllTicksData)
            {
                entry.Snapshot.Serialize(bw);
            }
        }

        private static bool DeserializeServerData(BinaryReader br, TickListDisplayer tickListDisplayer, TickDataDisplayer tickDataDisplayer)
        {
            // file info
            var header = br.ReadString();
            var formatVersion = br.ReadInt32();
            // note that date is provided but currently has no use
            var date = DateTime.FromBinary(br.ReadInt64());
            if (!header.Equals(ElympicsFileFormatName + ServerIndicator) || formatVersion != ElympicsFileFormatVersion)
                return false;

            // game info
            bool compatibilityCheck = true;
            var config = ElympicsConfig.LoadCurrentElympicsGameConfig();
            compatibilityCheck &= br.ReadString() == config.GameName;
            compatibilityCheck &= br.ReadString() == config.GameId;
            compatibilityCheck &= br.ReadString() == config.GameVersion;
            compatibilityCheck &= br.ReadInt32() == config.Players;
            if (!compatibilityCheck)
                return false;
            var major = br.ReadInt32();
            var minor = br.ReadInt32();
            var build = br.ReadInt32();
            // note that sdk version is provided but currently has no use
            // in the future it will be used in hardcoded conditional tree checking for versions with
            // breaking changes, deciding if we should allow older versions or not
            var fileElympicsVersion = new Version(major, minor, build);

            // time threshold
            ServerAnalyzerUtils.ExpectedTime = br.ReadSingle();
            float fileTickDurationInSeconds = ServerAnalyzerUtils.ExpectedTime / 1000;
            if (config.TickDuration != fileTickDurationInSeconds)
                UnityEngine.Debug.LogWarning($"CAUTION! Different TickDuration in game config ({config.TickDuration}) and in the input file({fileTickDurationInSeconds})!");

            // input view
            var isBots = new bool[br.ReadInt32()];
            for (int i = 0; i < isBots.Length; i++)
            {
                isBots[i] = br.ReadBoolean();
            }
            tickDataDisplayer.InitializeInputs(isBots);

            // snapshots
            tickListDisplayer.AllTicksData.Clear();
            var count = br.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                var snapshot = new ElympicsSnapshotWithMetadata();
                snapshot.Deserialize(br);
                tickListDisplayer.AddTick(snapshot);
            }

            return true;
        }
    }
}
#endif
