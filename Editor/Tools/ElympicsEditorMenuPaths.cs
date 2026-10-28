using Elympics.Editor.Launcher;

namespace Elympics.Editor
{
    internal static class ElympicsEditorMenuPaths
    {
        private const string ToolsMenuPath = "Tools/Elympics/";
        public const string ResetIdsMenuPath = ToolsMenuPath + "Reset Network Ids";
        public const string SetupMenuPath = ToolsMenuPath + "Select or Create Config";
        public const string BuildWindowsServer = ToolsMenuPath + "Build Windows Server";
        public const string BuildLinuxServer = ToolsMenuPath + "Build Linux Server";
        public const string BuildAndUploadServer = ToolsMenuPath + "Build and Upload Server";
        public const string ManageGamesInElympics = ToolsMenuPath + "Manage games in Elympics";
        public const string MatchLauncher = ToolsMenuPath + MatchLauncherWindow.EditorTitle;
        public const string AddPhysics2DSimulator = ToolsMenuPath + "Add 2D Physics Simulator";
        public const string TrustCustomApiHost = ToolsMenuPath + "Trust Custom API Host on This Machine";

        private const string GameObjectMenuPath = "GameObject/Elympics/";
        public const string MouseActionCreateElympicsSystem = GameObjectMenuPath + "ElympicsSystem";
        public const string MouseActionCreateElympicsLobby = GameObjectMenuPath + "ElympicsLobby";
    }
}
