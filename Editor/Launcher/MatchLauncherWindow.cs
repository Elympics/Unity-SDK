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
    /// Drives <see cref="ElympicsMatchLauncher"/> in play mode the way an out-of-process host would,
    /// and shows every host event it emits.
    /// </summary>
    internal sealed class MatchLauncherWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Elympics/" + EditorTitle;
        private const string EditorTitle = "Match Launcher";
        private const int MaxLogEntries = 500;

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

            EditorGUILayout.Space();
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
                EditorGUILayout.SelectableLabel(line, EditorStyles.wordWrappedMiniLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndScrollView();
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

        private static string DefaultGameId() => ElympicsConfig.Load()?.AvailableGames.FirstOrDefault(config => config != null)?.GameId ?? "";

        /// <summary>The wire form of <paramref name="kind"/> with every field unset, so the launch uses the defaults.</summary>
        private static string TemplateData(ElympicsHostLaunchKind kind) => JsonUtility.ToJson(ElympicsLaunchDtos.Create(kind), true);

        private static string TemplateOptions() => JsonUtility.ToJson(new ElympicsLaunchOptionsDto(), true);
    }
}
