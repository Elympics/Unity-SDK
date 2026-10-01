#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Elympics.Editor.Launcher
{
    /// <summary>
    /// Launches matches in the Editor: one click per mode with the selected game's <see cref="ElympicsGameConfig"/> defaults,
    /// or any launch as JSON, the way an out-of-process host would. Shows every host event the launcher emits.
    /// </summary>
    internal sealed class MatchLauncherWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Elympics/" + EditorTitle;
        private const string EditorTitle = "Match Launcher";
        private const int MaxLogEntries = 500;
        /// <summary>Room for the vertical scrollbar and margins when measuring wrapped log lines.</summary>
        private const float LogScrollbarWidth = 24f;

        [SerializeField] private ElympicsHostLaunchKind launchKind = ElympicsHostLaunchKind.SinglePlayer;
        [SerializeField] private string gameId = "";
        [SerializeField] private string dataJson = "";
        [SerializeField] private string optionsJson = "";
        [SerializeField] private List<string> log = new();

        private ElympicsMatchLauncher? _subscribed;
        private Vector2 _dataScroll;
        private Vector2 _optionsScroll;
        private Vector2 _logScroll;
        private int _requestCounter;
        private string? _launchModeSource;

        [MenuItem(MenuPath)]
        public static void OpenWindow() => GetWindow<MatchLauncherWindow>().titleContent = new GUIContent(EditorTitle);

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(gameId))
                gameId = DefaultGameId();
            if (string.IsNullOrEmpty(dataJson))
                dataJson = TemplateData(launchKind);
            if (string.IsNullOrEmpty(optionsJson))
                optionsJson = TemplateOptions();
            _launchModeSource = LaunchModeParameterSource();
        }

        private void OnFocus() => _launchModeSource = LaunchModeParameterSource();

        /// <summary>
        /// Where the launch mode parameter of this Editor comes from, or null if it is not set. With it set, the launcher
        /// rejects every launch, so the window's launch controls are disabled.
        /// </summary>
        private static string? LaunchModeParameterSource()
        {
            const string argument = "--elympics-launch-mode";
            if (CloneArguments.Read().Any(arg => arg.StartsWith(argument)))
                return "Launch mode is set by this clone's ParrelSync arguments (" + argument + "). Edit or clear them in the Clones Manager to launch from here.";
            if (Environment.GetCommandLineArgs().Any(arg => arg.StartsWith(argument)))
                return "Launch mode is set by this Editor's command line (" + argument + "). Restart the Editor without it to launch from here.";
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ELYMPICS_LAUNCH_MODE")))
                return "Launch mode is set by the ELYMPICS_LAUNCH_MODE environment variable. Restart the Editor without it to launch from here.";
            return null;
        }

        private void OnDisable() => Subscribe(null);

        // Keeps the subscription on the launcher of the current play session.
        private void OnInspectorUpdate()
        {
            var current = Application.isPlaying ? ElympicsMatchLauncher.Current : null;
            if (current != _subscribed)
            {
                Subscribe(current);
                Repaint();
            }
        }

        private void Subscribe(ElympicsMatchLauncher? launcher)
        {
            if (_subscribed != null)
            {
                _subscribed.HostEventEmitted -= OnHostEvent;
                _subscribed.StateChanged -= OnStateChanged;
            }
            _subscribed = launcher;
            if (_subscribed == null)
                return;
            _subscribed.HostEventEmitted += OnHostEvent;
            _subscribed.StateChanged += OnStateChanged;
        }

        private void OnStateChanged(MatchLauncherState state) => Repaint();

        private void OnHostEvent(string type, string requestId, string payloadJson)
        {
            var requestPart = string.IsNullOrEmpty(requestId) ? "" : $" [{requestId}]";
            var payloadPart = string.IsNullOrEmpty(payloadJson) ? "" : $" {payloadJson}";
            Append($"{DateTime.Now:HH:mm:ss.fff} ← {type}{requestPart}{payloadPart}");
        }

        private void Append(string line)
        {
            log.Add(line);
            if (log.Count > MaxLogEntries)
                log.RemoveRange(0, log.Count - MaxLogEntries);
            _logScroll.y = float.MaxValue;
            Repaint();
        }

        private void OnGUI()
        {
            var launcher = _subscribed;
            EditorGUILayout.LabelField("State", launcher == null ? "Not in play mode" : $"{launcher.State} (in match: {launcher.IsCurrentlyInMatch})", EditorStyles.boldLabel);

            if (_launchModeSource != null)
                EditorGUILayout.HelpBox(_launchModeSource, MessageType.Info);
            using (new EditorGUI.DisabledScope(_launchModeSource != null))
                DrawLaunchControls(launcher);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(launcher == null))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Abort / return to boot"))
                    SendWithId("abortMatch", id => launcher!.HandleHostAbort(id));
                if (GUILayout.Button("Ping"))
                    SendWithId("ping", id => launcher!.HandleHostPing(id));
                if (GUILayout.Button("Quit application"))
                    Send("quitApplication", () => launcher!.HandleHostQuit("Match Launcher window"));
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
                if (GUILayout.Button("Clear", GUILayout.Width(60)))
                    log.Clear();
            }
            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.ExpandHeight(true));
            foreach (var line in log)
            {
                // Wrapped lines (e.g. with a JSON payload) need their real height, or they overlap the next entry.
                var style = EditorStyles.wordWrappedMiniLabel;
                var height = style.CalcHeight(new GUIContent(line), position.width - LogScrollbarWidth);
                EditorGUILayout.SelectableLabel(line, style, GUILayout.Height(height));
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawLaunchControls(ElympicsMatchLauncher? launcher)
        {

            EditorGUILayout.Space();
            var game = SelectedGame();
            EditorGUILayout.LabelField("Quick launch", game != null ? $"{game.GameName}, with its config defaults" : "No game selected in ElympicsConfig", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(game == null || launcher is { State: not MatchLauncherState.Idle }))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("▶ Single player"))
                    QuickLaunch(ElympicsHostLaunchKind.SinglePlayer, game!);
                if (GUILayout.Button("▶ HR server"))
                    QuickLaunch(ElympicsHostLaunchKind.HalfRemoteServer, game!);
                if (GUILayout.Button("▶ HR client"))
                    QuickLaunch(ElympicsHostLaunchKind.HalfRemoteClient, game!);
                if (GUILayout.Button(new GUIContent("▶ Replay", game != null ? $"Replays {game.SnapshotFilePath}" : "")))
                    QuickLaunch(ElympicsHostLaunchKind.SnapshotReplay, game!);
            }
            if (game != null && !IsInBuildSettings(game.GameplayScene))
                EditorGUILayout.HelpBox($"Gameplay scene {game.GameplayScene} is not enabled in Build Settings, so launches fail with SceneLoadFailed. "
                    + "Add it in File > Build Settings.", MessageType.Error);
            if (game != null && !Application.runInBackground)
                EditorGUILayout.HelpBox("PlayerSettings \"Run In Background\" is off: half-remote matches stop simulating in out-of-focus Editor windows.", MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("JSON launch", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                var kind = (ElympicsHostLaunchKind)EditorGUILayout.EnumPopup("Match mode", launchKind);
                if (kind != launchKind)
                {
                    launchKind = kind;
                    dataJson = TemplateData(kind);
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                gameId = EditorGUILayout.TextField("Game ID", gameId);
                if (GUILayout.Button("First game", GUILayout.Width(80)))
                    gameId = DefaultGameId();
            }

            EditorGUILayout.LabelField($"Launch data ({launchKind} JSON, empty for defaults)");
            _dataScroll = EditorGUILayout.BeginScrollView(_dataScroll, GUILayout.MinHeight(100), GUILayout.MaxHeight(240));
            dataJson = EditorGUILayout.TextArea(dataJson, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Template"))
                    dataJson = TemplateData(launchKind);
                if (GUILayout.Button("Load file…"))
                    LoadDataFromFile();
            }

            EditorGUILayout.LabelField("Launch options (JSON, empty for defaults)");
            _optionsScroll = EditorGUILayout.BeginScrollView(_optionsScroll, GUILayout.MinHeight(60), GUILayout.MaxHeight(120));
            optionsJson = EditorGUILayout.TextArea(optionsJson, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(launcher == null))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Launch"))
                    Send($"launch{launchKind}", () => launcher!.HandleHostLaunch(launchKind, gameId, dataJson, optionsJson));
            }

        }

        private void Send(string command, Action send)
        {
            Append($"{DateTime.Now:HH:mm:ss.fff} → {command}");
            send();
        }

        private void SendWithId(string command, Action<string> send)
        {
            var requestId = $"window-{++_requestCounter}";
            Append($"{DateTime.Now:HH:mm:ss.fff} → {command} [{requestId}]");
            send(requestId);
        }

        private void LoadDataFromFile()
        {
            var path = EditorUtility.OpenFilePanel("Load launch data", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(path))
                dataJson = File.ReadAllText(path);
        }

        private static bool IsInBuildSettings(string scenePath) => EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == scenePath);

        private static ElympicsGameConfig? SelectedGame() => ElympicsConfig.Load()?.GetCurrentGameConfig();

        private void QuickLaunch(ElympicsHostLaunchKind kind, ElympicsGameConfig game)
        {
            Append($"{DateTime.Now:HH:mm:ss.fff} → {kind} of {game.GameName}{(Application.isPlaying ? "" : " (entering play mode)")}");
            EditorMatchLaunchQueue.Launch(kind, game.GameId);
        }

        private static string DefaultGameId() => ElympicsConfig.Load()?.AvailableGames.FirstOrDefault(config => config != null)?.GameId ?? "";

        /// <summary>The wire form of <paramref name="kind"/> with every field unset, so the launch uses the defaults.</summary>
        private static string TemplateData(ElympicsHostLaunchKind kind) => JsonUtility.ToJson(ElympicsLaunchDtos.Create(kind), true);

        private static string TemplateOptions() => JsonUtility.ToJson(new ElympicsLaunchOptionsDto(), true);
    }
}
