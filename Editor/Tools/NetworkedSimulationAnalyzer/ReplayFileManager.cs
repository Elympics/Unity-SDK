using System;
using System.IO;
using MessagePack;
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
                    _ = EditorUtility.DisplayDialog("Wrong replay file", "Could not read provided file or it was uncompatible, outdated or had wrong format. " +
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
                bw.Write(isBot);

            // snapshots
            bw.Write(tickListDisplayer.AllTicksData.Count);
            foreach (var entry in tickListDisplayer.AllTicksData)
                MessagePackSerializer.Serialize(bw.BaseStream, entry.Snapshot);
        }

        private static bool DeserializeServerData(BinaryReader br, TickListDisplayer tickListDisplayer, TickDataDisplayer tickDataDisplayer)
        {
            // file info
            var header = br.ReadString();
            var formatVersion = br.ReadInt32();

            // note that date is provided but currently has no use
            _ = DateTime.FromBinary(br.ReadInt64());
            if (!header.Equals(ElympicsFileFormatName + ServerIndicator) || formatVersion != ElympicsFileFormatVersion)
                return false;

            // game info
            var compatibilityCheck = true;
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
            _ = new Version(major, minor, build);

            // time threshold
            ServerAnalyzerUtils.ExpectedTime = br.ReadSingle();
            var fileTickDurationInSeconds = ServerAnalyzerUtils.ExpectedTime / 1000;
            if (config.TickDuration != fileTickDurationInSeconds)
                ElympicsLogger.LogWarning($"Different tick duration in game config ({config.TickDuration}) "
                    + $"and in the input replay file ({fileTickDurationInSeconds})!");

            // input view
            var isBots = new bool[br.ReadInt32()];
            for (var i = 0; i < isBots.Length; i++)
                isBots[i] = br.ReadBoolean();
            tickDataDisplayer.InitializeInputs(isBots);

            // snapshots
            tickListDisplayer.AllTicksData.Clear();
            var count = br.ReadInt32();
            for (var i = 0; i < count; i++)
                tickListDisplayer.AddTick(MessagePackSerializer.Deserialize<ElympicsSnapshotWithMetadata>(br.BaseStream));

            return true;
        }
    }
}
