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

        [SerializeField] private string requestJson = "";
        [SerializeField] private List<string> log = new();

        private ElympicsMatchLauncher? _subscribed;
        private Vector2 _requestScroll;
        private Vector2 _logScroll;
        private int _requestCounter;

        [MenuItem(MenuPath)]
        public static void OpenWindow() => GetWindow<MatchLauncherWindow>().titleContent = new GUIContent(EditorTitle);

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(requestJson))
                requestJson = TemplateRequest();
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
            EditorGUILayout.LabelField("Launch request (ElympicsLaunchRequest JSON)");
            _requestScroll = EditorGUILayout.BeginScrollView(_requestScroll, GUILayout.MinHeight(120), GUILayout.MaxHeight(260));
            requestJson = EditorGUILayout.TextArea(requestJson, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Template"))
                    requestJson = TemplateRequest();
                if (GUILayout.Button("Load file…"))
                    LoadRequestFromFile();
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(launcher == null))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Launch"))
                    Send("launchMatch", () => launcher!.HandleHostLaunch(requestJson));
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

        private void LoadRequestFromFile()
        {
            var path = EditorUtility.OpenFilePanel("Load launch request", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(path))
                requestJson = File.ReadAllText(path);
        }

        /// <summary>A SinglePlayer request for the first available game: the smallest request that launches anything.</summary>
        private static string TemplateRequest()
        {
            var game = ElympicsConfig.Load()?.AvailableGames.FirstOrDefault(config => config != null);
            var request = new ElympicsLaunchRequest
            {
                gameId = game != null ? game.GameId : "",
                matchMode = nameof(ElympicsSystems.Internal.JoinedMatchMode.SinglePlayer),
            };
            return JsonUtility.ToJson(request, true);
        }
    }
}
