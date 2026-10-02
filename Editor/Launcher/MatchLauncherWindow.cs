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
    internal sealed class MatchLauncherWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Elympics/" + EditorTitle;
        private const string EditorTitle = "Match Launcher";
        private const int MaxLogEntries = 500;
        /// <summary>A UI Toolkit text element can't have more than 65535 vertices (4 per character).</summary>
        private const int MaxShownLogCharacters = 12000;
        private const long UpdateIntervalMs = 200;
        private const string OtherGame = "Other";

        private static readonly ElympicsHostLaunchKind[] LaunchKinds = (ElympicsHostLaunchKind[])Enum.GetValues(typeof(ElympicsHostLaunchKind));

        [SerializeField] private VisualTreeAsset? windowUxml;

        [SerializeField] private ElympicsHostLaunchKind editedKind = ElympicsHostLaunchKind.SinglePlayer;
        [SerializeField] private string gameId = "";
        [SerializeField] private List<string> dataJsons = new();
        [SerializeField] private string optionsJson = "";
        [SerializeField] private List<string> log = new();

        private ElympicsMatchLauncher? _subscribed;
        private int _requestCounter;

        private Label? _state;
        private HelpBox _launchModeNote = null!;
        private VisualElement _launchControls = null!;
        private DropdownField _game = null!;
        private TextField _gameIdField = null!;
        private readonly List<string> _gameIds = new();
        private Label _unknownGame = null!;
        private HelpBox _buildSettingsError = null!;
        private HelpBox _runInBackgroundWarning = null!;
        private readonly Dictionary<ElympicsHostLaunchKind, ToolbarToggle> _tabs = new();
        private TextField _dataJson = null!;
        private Button _launchButton = null!;
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
            _unknownGame = root.Q<Label>("unknown-game");
            _buildSettingsError = root.Q<HelpBox>("build-settings-error");
            _runInBackgroundWarning = root.Q<HelpBox>("run-in-background-warning");
            _matchControls = root.Q<VisualElement>("match-controls");
            _logScroll = root.Q<ScrollView>("log-scroll");
            _log = root.Q<TextField>("log");

            _game = root.Q<DropdownField>("game");
            _ = _game.RegisterValueChangedCallback(change => SelectGame(change.newValue));
            _gameIdField = root.Q<TextField>("game-id");
            _gameIdField.SetValueWithoutNotify(gameId);
            _ = _gameIdField.RegisterValueChangedCallback(change =>
            {
                gameId = change.newValue;
                ShowSelectedGame();
            });

            _dataJson = root.Q<TextField>("data-json");
            _dataJson.SetValueWithoutNotify(dataJsons[(int)editedKind]);
            _ = _dataJson.RegisterValueChangedCallback(change => dataJsons[(int)editedKind] = change.newValue);
            _tabs.Clear();
            foreach (var kind in LaunchKinds)
            {
                var tab = root.Q<ToolbarToggle>($"tab-{kind}");
                _tabs[kind] = tab;
                // A tab can't be closed by clicking it again: one tab is always open.
                _ = tab.RegisterValueChangedCallback(change => SelectTab(change.newValue ? kind : editedKind));
            }
            root.Q<Button>("data-template").clicked += () => _dataJson.value = TemplateData(editedKind);
            root.Q<Button>("data-load").clicked += LoadDataFromFile;

            var optionsField = root.Q<TextField>("options-json");
            optionsField.SetValueWithoutNotify(optionsJson);
            _ = optionsField.RegisterValueChangedCallback(change => optionsJson = change.newValue);
            root.Q<Button>("options-template").clicked += () => optionsField.value = TemplateOptions();

            _launchButton = root.Q<Button>("launch");
            _launchButton.clicked += () => Launch(editedKind);
            SelectTab(editedKind);

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

            UpdateGameChoices();
            var game = SelectedGame();
            _unknownGame.style.visibility = game == null ? Visibility.Visible : Visibility.Hidden;
            _unknownGame.text = $"No game with ID \"{gameId}\" in ElympicsConfig";
            SetShown(_buildSettingsError, game != null && !IsInBuildSettings(game.GameplayScene));
            if (game != null)
                _buildSettingsError.text = $"Gameplay scene {game.GameplayScene} is not enabled in Build Settings, so launches fail with SceneLoadFailed. "
                    + "Add it in File > Build Settings.";
            SetShown(_runInBackgroundWarning, !Application.runInBackground);

            _launchButton.SetEnabled(launcher == null || launcher.State == MatchLauncherState.Idle);
            _matchControls.SetEnabled(launcher != null);
        }

        private void SelectTab(ElympicsHostLaunchKind kind)
        {
            editedKind = kind;
            foreach (var (tabKind, tab) in _tabs)
                tab.SetValueWithoutNotify(tabKind == kind);
            _dataJson.SetValueWithoutNotify(dataJsons[(int)kind]);
            _launchButton.text = $"▶ {_tabs[kind].text}";
        }

        private static void SetShown(VisualElement element, bool shown) => element.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;

        private static ElympicsMatchLauncher? CurrentLauncher() => EditorApplication.isPlaying ? ElympicsMatchLauncher.Current : null;

        /// <summary>With the launch mode parameter set, the launcher rejects every launch.</summary>
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

        private void Launch(ElympicsHostLaunchKind kind)
        {
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

        private void UpdateGameChoices()
        {
            var games = ElympicsConfig.Load()?.AvailableGames.Where(config => config != null).ToList() ?? new List<ElympicsGameConfig>();
            var ids = games.Select(game => game.GameId).ToList();
            var labels = games.Select(game => games.Count(other => other.GameName == game.GameName) > 1 ? $"{game.GameName} ({game.GameId})" : game.GameName)
                .Append(OtherGame)
                .ToList();
            if (!ids.SequenceEqual(_gameIds) || !labels.SequenceEqual(_game.choices))
            {
                _gameIds.Clear();
                _gameIds.AddRange(ids);
                _game.choices = labels;
            }
            ShowSelectedGame();
        }

        private void ShowSelectedGame()
        {
            var index = _gameIds.IndexOf(gameId);
            _game.SetValueWithoutNotify(index >= 0 ? _game.choices[index] : OtherGame);
        }

        private void SelectGame(string label)
        {
            var index = _game.choices.IndexOf(label);
            if (index >= 0 && index < _gameIds.Count)
                _gameIdField.value = _gameIds[index];
            else if (_gameIds.Contains(gameId))
            {
                _gameIdField.value = "";
                _gameIdField.Focus();
            }
        }

        private ElympicsGameConfig? SelectedGame() => ElympicsConfig.Load()?.AvailableGames.FirstOrDefault(config => config != null && config.GameId == gameId);

        private static string DefaultGameId() => ElympicsConfig.Load()?.AvailableGames.FirstOrDefault(config => config != null)?.GameId ?? "";

        /// <summary>Every field is unset, so the launch uses the game config's values.</summary>
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
