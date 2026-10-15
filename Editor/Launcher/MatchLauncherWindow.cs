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
        public const string EditorTitle = "Match Launcher";
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
        private bool _logRefreshPending;

        [MenuItem(ElympicsEditorMenuPaths.MatchLauncher)]
        public static void OpenWindow() => GetWindow<MatchLauncherWindow>().titleContent = new GUIContent(EditorTitle);

        private void OnEnable()
        {
            // The saved ID can be stale, e.g. after the game was removed from the config.
            if (string.IsNullOrEmpty(gameId) || (ElympicsConfig.Load() is { } config && GetSelectedGame(config) == null))
                gameId = GetDefaultGameId();
            while (dataJsons.Count < LaunchKinds.Length)
                dataJsons.Add(GetTemplateData((ElympicsHostLaunchKind)dataJsons.Count));
            if (string.IsNullOrEmpty(optionsJson))
                optionsJson = GetTemplateOptions();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.projectChanged += UpdateEnvironment;
            EditorBuildSettings.sceneListChanged += UpdateEnvironment;
            // After the domain reload of entering play mode, subscribe before the launch queue sends its launch.
            Subscribe(CurrentLauncher);
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.projectChanged -= UpdateEnvironment;
            EditorBuildSettings.sceneListChanged -= UpdateEnvironment;
            Subscribe(null);
        }

        private void OnFocus() => UpdateEnvironment();

        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            Subscribe(CurrentLauncher);
            UpdateEnvironment();
        }

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
                UpdateEnvironment();
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
            root.Q<Button>("data-template").clicked += () => _dataJson.value = GetTemplateData(editedKind);
            root.Q<Button>("data-load").clicked += LoadDataFromFile;

            var optionsField = root.Q<TextField>("options-json");
            optionsField.SetValueWithoutNotify(optionsJson);
            _ = optionsField.RegisterValueChangedCallback(change => optionsJson = change.newValue);
            root.Q<Button>("options-template").clicked += () => optionsField.value = GetTemplateOptions();

            _launchButton = root.Q<Button>("launch");
            _launchButton.clicked += () => Launch(editedKind);
            SelectTab(editedKind);

            root.Q<Button>("return-to-boot").clicked += () => SendWithId("returnToBoot", id => _subscribed?.HandleHostCommand(ElympicsHostCommandKind.ReturnToBoot, id));
            root.Q<Button>("ping").clicked += () => SendWithId("ping", id => _subscribed?.HandleHostCommand(ElympicsHostCommandKind.Ping, id));
            root.Q<Button>("quit").clicked += () => Send("quitApplication", () => _subscribed?.HandleHostCommand(ElympicsHostCommandKind.QuitApplication, "Match Launcher window"));
            root.Q<Button>("log-copy").clicked += () => EditorGUIUtility.systemCopyBuffer = string.Join("\n", log);
            root.Q<Button>("log-clear").clicked += () =>
            {
                log.Clear();
                ShowLog();
            };

            ShowLog();
            UpdateEnvironment();
            RefreshStateUi();
            // The launcher is recreated for each play session; StateChanged drives the UI between swaps.
            _ = root.schedule.Execute(() => Subscribe(CurrentLauncher)).Every(UpdateIntervalMs);
        }

        private void RefreshStateUi()
        {
            if (_state == null)
                return;
            var launcher = _subscribed;
            _state.text = launcher == null ? "Not in play mode" : $"{launcher.State} (in match: {launcher.IsCurrentlyInMatch})";
            _launchButton.SetEnabled(launcher == null || launcher.State == MatchLauncherState.Idle);
            _matchControls.SetEnabled(launcher != null);
        }

        /// <summary>Reads config, Build Settings and arguments, so it runs on changes and focus instead of every tick.</summary>
        private void UpdateEnvironment()
        {
            if (_state == null)
                return;
            var launchModeSource = GetLaunchModeParameterSource();
            SetShown(_launchModeNote, launchModeSource != null);
            _launchModeNote.text = launchModeSource ?? "";
            _launchControls.SetEnabled(launchModeSource == null);

            var config = ElympicsConfig.Load();
            UpdateGameChoices(config);
            var game = GetSelectedGame(config);
            _unknownGame.style.visibility = game == null ? Visibility.Visible : Visibility.Hidden;
            _unknownGame.text = $"No game with ID \"{gameId}\" in ElympicsConfig";
            SetShown(_buildSettingsError, game != null && !IsInBuildSettings(game.GameplayScene));
            if (game != null)
                _buildSettingsError.text = $"Gameplay scene {game.GameplayScene} is not enabled in Build Settings, so launches fail with SceneLoadFailed. "
                    + "Add it in File > Build Settings.";
            SetShown(_runInBackgroundWarning, !Application.runInBackground);
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

        private static ElympicsMatchLauncher? CurrentLauncher => EditorApplication.isPlaying ? ElympicsMatchLauncher.Current : null;

        /// <summary>With the launch mode parameter set, the launcher rejects every launch.</summary>
        private static string? GetLaunchModeParameterSource()
        {
            var parameter = ApplicationParameters.Parameters.LaunchMode;
            var argument = "--" + parameter.LongArgumentName;
            if (PlayModeArguments.Read().Any(arg => arg.StartsWith(argument)))
                return "Launch mode is set for this Multiplayer Play Mode virtual player (" + argument + "). Give it the "
                    + PlayModeArguments.TagPrefix + PlayModeArguments.ManualTag + " tag to launch from here.";
            if (Environment.GetCommandLineArgs().Any(arg => arg.StartsWith(argument)))
                return "Launch mode is set by this Editor's command line (" + argument + "). Restart the Editor without it to launch from here.";
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(parameter.EnvironmentVariableName)))
                return "Launch mode is set by the " + parameter.EnvironmentVariableName + " environment variable. Restart the Editor without it to launch from here.";
            return null;
        }

        private void Subscribe(ElympicsMatchLauncher? launcher)
        {
            if (launcher == _subscribed)
                return;
            if (_subscribed != null)
            {
                _subscribed.HostEventEmitted -= OnHostEvent;
                _subscribed.StateChanged -= OnLauncherStateChanged;
            }
            _subscribed = launcher;
            if (_subscribed != null)
            {
                _subscribed.HostEventEmitted += OnHostEvent;
                _subscribed.StateChanged += OnLauncherStateChanged;
            }
            RefreshStateUi();
        }

        private void OnLauncherStateChanged(MatchLauncherState state) => RefreshStateUi();

        private void OnHostEvent(string type, string requestId, string payloadJson)
        {
            var requestPart = string.IsNullOrEmpty(requestId) ? "" : $" [{requestId}]";
            var payloadPart = string.IsNullOrEmpty(payloadJson) ? "" : $" {payloadJson}";
            Append($"{DateTime.Now:HH:mm:ss.fff} ← {type}{requestPart}{payloadPart}");
        }

        private void Launch(ElympicsHostLaunchKind kind)
        {
            var wasPlaying = EditorApplication.isPlaying;
            Append($"{DateTime.Now:HH:mm:ss.fff} → launch{kind} {gameId}");
            if (EditorMatchLaunchQueue.Launch(kind, gameId, dataJsons[(int)kind], optionsJson) is { } refusal)
                Append($"{DateTime.Now:HH:mm:ss.fff} Cancelled: {refusal}");
            else if (!wasPlaying)
                Append($"{DateTime.Now:HH:mm:ss.fff} Entering play mode");
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
            if (_log == null || _logRefreshPending)
                return;
            _logRefreshPending = true;
            _ = _log.schedule.Execute(ShowLog);
        }

        private void ShowLog()
        {
            _logRefreshPending = false;
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

        private static bool IsInBuildSettings(string scenePath) => ElympicsLauncherBuildValidation.GetEnabledBuildScenes().Contains(scenePath);

        private void UpdateGameChoices(ElympicsConfig? config)
        {
            var games = config?.AvailableGames.Where(config => config != null).ToList() ?? new List<ElympicsGameConfig>();
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

        private ElympicsGameConfig? GetSelectedGame(ElympicsConfig? config) => config?.AvailableGames.FirstOrDefault(config => config != null && config.GameId == gameId);

        private static string GetDefaultGameId() => ElympicsConfig.Load()?.AvailableGames.FirstOrDefault(config => config != null)?.GameId ?? "";

        /// <summary>Every field is unset, so the launch uses the game config's values.</summary>
        private string GetTemplateData(ElympicsHostLaunchKind kind)
        {
            var dto = ElympicsLaunchDtos.Create(kind);
            if (dto is SnapshotReplayLaunchDataDto replay && GetSelectedGame(ElympicsConfig.Load()) is { } game)
                replay.path = game.SnapshotFilePath;
            return JsonUtility.ToJson(dto, true);
        }

        private static string GetTemplateOptions() => JsonUtility.ToJson(new ElympicsLaunchOptionsDto(), true);
    }
}
