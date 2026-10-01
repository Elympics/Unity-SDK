#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Elympics.Editor.Launcher
{
    /// <summary>
    /// Launches matches in the Editor the way an out-of-process host would: each mode with its own data JSON, the shared
    /// options JSON and the game ID set in the window. Shows every host event the launcher emits.
    /// </summary>
    internal sealed class MatchLauncherWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Elympics/" + EditorTitle;
        private const string EditorTitle = "Match Launcher";
        private const int MaxLogEntries = 500;
        /// <summary>
        /// The log field shows at most this many of the latest characters: a UI Toolkit text element can't have more than
        /// 65535 vertices (4 per character). "Copy all" copies the whole log.
        /// </summary>
        private const int MaxShownLogCharacters = 12000;
        private const long UpdateIntervalMs = 200;

        private static readonly ElympicsHostLaunchKind[] LaunchKinds = (ElympicsHostLaunchKind[])Enum.GetValues(typeof(ElympicsHostLaunchKind));

        [SerializeField] private VisualTreeAsset? windowUxml;

        [SerializeField] private ElympicsHostLaunchKind editedKind = ElympicsHostLaunchKind.SinglePlayer;
        [SerializeField] private string gameId = "";
        /// <summary>The data JSON of each mode, indexed by <see cref="ElympicsHostLaunchKind"/>.</summary>
        [SerializeField] private List<string> dataJsons = new();
        [SerializeField] private string optionsJson = "";
        [SerializeField] private List<string> log = new();

        private ElympicsMatchLauncher? _subscribed;
        private int _requestCounter;

        private Label? _state;
        private HelpBox _launchModeNote = null!;
        private VisualElement _launchControls = null!;
        private Label _gameName = null!;
        private HelpBox _buildSettingsError = null!;
        private HelpBox _runInBackgroundWarning = null!;
        private EnumField? _dataMode;
        private TextField _dataJson = null!;
        private VisualElement _launchButtons = null!;
        private VisualElement _matchControls = null!;
        private ScrollView _logScroll = null!;
        private TextField? _log;

        [MenuItem(MenuPath)]
        public static void OpenWindow() => GetWindow<MatchLauncherWindow>().titleContent = new GUIContent(EditorTitle);

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(gameId))
                gameId = DefaultGameId();
            while (dataJsons.Count < LaunchKinds.Length)
                dataJsons.Add(TemplateData((ElympicsHostLaunchKind)dataJsons.Count));
            if (string.IsNullOrEmpty(optionsJson))
                optionsJson = TemplateOptions();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            // After the domain reload of entering play mode, subscribe before the launch queue sends its launch.
            Subscribe(CurrentLauncher());
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Subscribe(null);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange change) => Subscribe(CurrentLauncher());

        private void CreateGUI()
        {
            if (windowUxml == null)
            {
                rootVisualElement.Add(new HelpBox($"{nameof(MatchLauncherWindow)}.uxml is not assigned to the window script.", HelpBoxMessageType.Error));
                return;
            }
            windowUxml.CloneTree(rootVisualElement);
            var root = rootVisualElement;

            _state = root.Q<Label>("state");
            _launchModeNote = root.Q<HelpBox>("launch-mode-note");
            _launchControls = root.Q<VisualElement>("launch-controls");
            _gameName = root.Q<Label>("game-name");
            _buildSettingsError = root.Q<HelpBox>("build-settings-error");
            _runInBackgroundWarning = root.Q<HelpBox>("run-in-background-warning");
            _launchButtons = root.Q<VisualElement>("launch-buttons");
            _matchControls = root.Q<VisualElement>("match-controls");
            _logScroll = root.Q<ScrollView>("log-scroll");
            _log = root.Q<TextField>("log");

            var gameIdField = root.Q<TextField>("game-id");
            gameIdField.SetValueWithoutNotify(gameId);
            _ = gameIdField.RegisterValueChangedCallback(change => gameId = change.newValue);
            root.Q<Button>("first-game").clicked += () => gameIdField.value = DefaultGameId();

            _dataJson = root.Q<TextField>("data-json");
            _dataJson.SetValueWithoutNotify(dataJsons[(int)editedKind]);
            _ = _dataJson.RegisterValueChangedCallback(change => dataJsons[(int)editedKind] = change.newValue);
            _dataMode = root.Q<EnumField>("data-mode");
            _dataMode.Init(editedKind);
            _ = _dataMode.RegisterValueChangedCallback(change =>
            {
                editedKind = (ElympicsHostLaunchKind)change.newValue;
                _dataJson.SetValueWithoutNotify(dataJsons[(int)editedKind]);
            });
            root.Q<Button>("data-template").clicked += () => _dataJson.value = TemplateData(editedKind);
            root.Q<Button>("data-load").clicked += LoadDataFromFile;

            var optionsField = root.Q<TextField>("options-json");
            optionsField.SetValueWithoutNotify(optionsJson);
            _ = optionsField.RegisterValueChangedCallback(change => optionsJson = change.newValue);
            root.Q<Button>("options-template").clicked += () => optionsField.value = TemplateOptions();

            foreach (var kind in LaunchKinds)
                root.Q<Button>($"launch-{kind}").clicked += () => Launch(kind);

            root.Q<Button>("abort").clicked += () => SendWithId("abortMatch", id => _subscribed?.HandleHostAbort(id));
            root.Q<Button>("ping").clicked += () => SendWithId("ping", id => _subscribed?.HandleHostPing(id));
            root.Q<Button>("quit").clicked += () => Send("quitApplication", () => _subscribed?.HandleHostQuit("Match Launcher window"));
            root.Q<Button>("log-copy").clicked += () => EditorGUIUtility.systemCopyBuffer = string.Join("\n", log);
            root.Q<Button>("log-clear").clicked += () =>
            {
                log.Clear();
                ShowLog();
            };

            ShowLog();
            UpdateState();
            _ = root.schedule.Execute(UpdateState).Every(UpdateIntervalMs);
        }

        /// <summary>Refreshes everything that depends on play mode, the launcher state, the config and the launch mode parameter.</summary>
        private void UpdateState()
        {
            Subscribe(CurrentLauncher());
            if (_state == null)
                return;
            var launcher = _subscribed;
            _state.text = launcher == null ? "Not in play mode" : $"{launcher.State} (in match: {launcher.IsCurrentlyInMatch})";

            var launchModeSource = LaunchModeParameterSource();
            SetShown(_launchModeNote, launchModeSource != null);
            _launchModeNote.text = launchModeSource ?? "";
            _launchControls.SetEnabled(launchModeSource == null);

            var game = SelectedGame();
            _gameName.text = game != null ? game.GameName : $"No game with ID \"{gameId}\" in ElympicsConfig";
            SetShown(_buildSettingsError, game != null && !IsInBuildSettings(game.GameplayScene));
            if (game != null)
                _buildSettingsError.text = $"Gameplay scene {game.GameplayScene} is not enabled in Build Settings, so launches fail with SceneLoadFailed. "
                    + "Add it in File > Build Settings.";
            SetShown(_runInBackgroundWarning, !Application.runInBackground);

            _launchButtons.SetEnabled(launcher == null || launcher.State == MatchLauncherState.Idle);
            _matchControls.SetEnabled(launcher != null);
        }

        private static void SetShown(VisualElement element, bool shown) => element.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;

        private static ElympicsMatchLauncher? CurrentLauncher() => EditorApplication.isPlaying ? ElympicsMatchLauncher.Current : null;

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

        private void Subscribe(ElympicsMatchLauncher? launcher)
        {
            if (launcher == _subscribed)
                return;
            if (_subscribed != null)
                _subscribed.HostEventEmitted -= OnHostEvent;
            _subscribed = launcher;
            if (_subscribed != null)
                _subscribed.HostEventEmitted += OnHostEvent;
        }

        private void OnHostEvent(string type, string requestId, string payloadJson)
        {
            var requestPart = string.IsNullOrEmpty(requestId) ? "" : $" [{requestId}]";
            var payloadPart = string.IsNullOrEmpty(payloadJson) ? "" : $" {payloadJson}";
            Append($"{DateTime.Now:HH:mm:ss.fff} ← {type}{requestPart}{payloadPart}");
        }

        /// <summary>Sends the launch of <paramref name="kind"/> with its data JSON, entering play mode first if needed. Shows the sent data in the editor.</summary>
        private void Launch(ElympicsHostLaunchKind kind)
        {
            if (_dataMode != null)
                _dataMode.value = kind;
            var entering = EditorApplication.isPlaying ? "" : " (entering play mode)";
            Append($"{DateTime.Now:HH:mm:ss.fff} → launch{kind} {gameId}{entering}");
            if (!EditorMatchLaunchQueue.Launch(kind, gameId, dataJsons[(int)kind], optionsJson))
                Append($"{DateTime.Now:HH:mm:ss.fff} Cancelled: the open scenes were not saved.");
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

        private void Append(string line)
        {
            log.Add(line);
            if (log.Count > MaxLogEntries)
                log.RemoveRange(0, log.Count - MaxLogEntries);
            ShowLog();
        }

        /// <summary>Shows the log as one text, so any part of it can be selected and copied.</summary>
        private void ShowLog()
        {
            if (_log == null)
                return;
            var text = string.Join("\n", log);
            if (text.Length > MaxShownLogCharacters)
            {
                var start = text.IndexOf('\n', text.Length - MaxShownLogCharacters);
                text = "(older entries: Copy all)\n" + text[(start < 0 ? text.Length - MaxShownLogCharacters : start + 1)..];
            }
            _log.SetValueWithoutNotify(text);
            // Scroll once the new text is laid out.
            _ = _logScroll.schedule.Execute(() => _logScroll.scrollOffset = new Vector2(0, float.MaxValue));
        }

        private void LoadDataFromFile()
        {
            var path = EditorUtility.OpenFilePanel("Load launch data", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(path))
                _dataJson.value = File.ReadAllText(path);
        }

        private static bool IsInBuildSettings(string scenePath) => EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == scenePath);

        private ElympicsGameConfig? SelectedGame() => ElympicsConfig.Load()?.AvailableGames.FirstOrDefault(config => config != null && config.GameId == gameId);

        private static string DefaultGameId() => ElympicsConfig.Load()?.AvailableGames.FirstOrDefault(config => config != null)?.GameId ?? "";

        /// <summary>
        /// The wire form of <paramref name="kind"/> with every field unset, so the launch uses the game config's values.
        /// A replay has no default, so its template points at the selected game's snapshot file.
        /// </summary>
        private string TemplateData(ElympicsHostLaunchKind kind)
        {
            var dto = ElympicsLaunchDtos.Create(kind);
            if (dto is SnapshotReplayLaunchDataDto replay && SelectedGame() is { } game)
                replay.path = game.SnapshotFilePath;
            return JsonUtility.ToJson(dto, true);
        }

        private static string TemplateOptions() => JsonUtility.ToJson(new ElympicsLaunchOptionsDto(), true);
    }
}
