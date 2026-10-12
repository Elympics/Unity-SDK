using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elympics.Core.Logger;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

#nullable enable

namespace Elympics.Editor.Config
{
    internal class ManageGamesInElympicsWindow : EditorWindow
    {
        #region Style classes

        private const string AccountGameRowClass = "elympics-account-game-row";
        private const string AccountGameNameClass = "elympics-account-game-name";
        private const string AccountGameIdClass = "elympics-account-game-id";

        #endregion

        #region Labels

        private const string WindowTitle = "Manage games in Elympics";

        private const string NotSyncedYetText = "Click Synchronize button to retrieve available regions for active game.";
        private const string NoRegionsAvailableText = "No available regions for active game.";
        private const string RegionsAvailableText = "Available regions for active game:";

        #endregion

        #region Client build uploads

        private const string ClientBuildPathKey = "ElympicsClientBuildPathForUpload";
        private const string StreamingAssetsPathKey = "ElympicsStreamingAssetsPathForUpload";
        private const string StreamingAssetsVersionKey = "ElympicsStreamingAssetsVersionForUpload";
        private const string StreamingAssetsLayoutKey = "ElympicsStreamingAssetsLayoutForUpload";

        private static readonly StreamingAssetsLayout[] StreamingAssetsLayouts = (StreamingAssetsLayout[])Enum.GetValues(typeof(StreamingAssetsLayout));

        [SerializeField] private string? clientBuildPath;
        [SerializeField] private string? clientVersionName;
        [SerializeField] private string? streamingAssetsPath;
        [SerializeField] private string? streamingAssetsVersion;
        [SerializeField] private StreamingAssetsLayout streamingAssetsLayout;

        #endregion

        private const int TickIntervalMs = 200;

        private static readonly Vector2 DefaultWindowSize = new(500, 900);
        private static readonly Vector2 MinWindowSize = new(250, 500);
        private static readonly Vector2 MaxWindowSize = new(1000, 1800);

        private static readonly Color WrongUriColor = new(0xEC / 255f, 0xC7 / 255f, 0x0C / 255f);
        private static readonly Color ConnectingColor = new(0x00 / 255f, 0x61 / 255f, 0xE1 / 255f);
        private static readonly Color ConnectedColor = new(0x63 / 255f, 0xDE / 255f, 0x4B / 255f);
        private static readonly Color NotConnectedColor = new(0xE1 / 255f, 0x00 / 255f, 0x1E / 255f);

        // NOTE: All serialized fields survive domain reloads

        [SerializeField] private VisualTreeAsset? windowUxml;

        [SerializeField] private ElympicsConfig? config;

        private class SerializedConfig
        {
            public SerializedObject Config { get; }
            public SerializedProperty AvailableGames { get; }
            public SerializedProperty ElympicsWebEndpoint { get; }
            public SerializedProperty ElympicsGameServersEndpoint { get; }

            public SerializedConfig(SerializedObject config)
            {
                Config = config;

                AvailableGames = Config.FindProperty("availableGames");
                ElympicsWebEndpoint = Config.FindProperty("elympicsWebEndpoint");
                ElympicsGameServersEndpoint = Config.FindProperty("elympicsGameServersEndpoint");
            }

            public void ApplyModifiedProperties() => Config.ApplyModifiedProperties();
        }

        private SerializedConfig? _serializedConfig;

        private IVisualElementScheduledItem? _tick;

        private EditorEndpointChecker? _webEndpointChecker;
        private EditorEndpointChecker? _gameServersEndpointChecker;

        private (string Text, Color Color)? _lastWebIndicator;
        private (string Text, Color Color)? _lastGameServersIndicator;

        private string[]? _availableRegions;
        private List<ElympicsWebIntegration.GameResponseModel> _accountGames = new();

        private UnityEditor.Editor? _gameConfigEditor;
        private bool? _lastIsLogin;

        // NOT SERIALIZED for rebuilding the visual tree after a domain reload.
        private ElympicsConfig? _lastRebuildConfig;

        private class VisualElements
        {
            public VisualElement LoginView { get; }
            public VisualElement ManageView { get; }
            public Label LoginWebEndpointStatus { get; }
            public Label WebEndpointStatus { get; }
            public Label GameServersEndpointStatus { get; }
            public Label LoggedAs { get; }
            public VisualElement AccountGamesSection { get; }
            public VisualElement AccountGamesContainer { get; }
            public VisualElement RegionsSection { get; }
            public Label RegionsInfo { get; }
            public VisualElement RegionsContainer { get; }
            public VisualElement NoGameConfigSection { get; }
            public Button ImportGamesButton { get; }
            public VisualElement GameConfigSection { get; }
            public Label GameConfigHeader { get; }
            public VisualElement GameConfigRoot { get; }
            public Label ManageGameHeader { get; }
            public ListView AvailableGamesList { get; }
            public Button CreateFirstConfigButton { get; }
            public TextField LoginUsername { get; }
            public TextField LoginPassword { get; }
            public Button LoginButton { get; }
            public Button LogoutButton { get; }
            public TextField LoginWebEndpoint { get; }
            public TextField WebEndpoint { get; }
            public TextField GameServersEndpoint { get; }
            public Button SynchronizeButton { get; }
            public TextField ClientVersion { get; }
            public TextField BuildPath { get; }
            public Button BuildUploadServerButton { get; }
            public Button LogVersionsButton { get; }
            public Button UploadClientButton { get; }
            public DropdownField StreamingAssetsLayoutField { get; }
            public TextField StreamingAssetsVersionField { get; }
            public TextField StreamingAssetsPathField { get; }
            public Button UploadStreamingAssetsButton { get; }

            public VisualElements(VisualElement root)
            {
                LoginView = root.Q<VisualElement>("login-view");
                ManageView = root.Q<VisualElement>("manage-view");
                LoginWebEndpointStatus = root.Q<Label>("login-web-endpoint-status");
                WebEndpointStatus = root.Q<Label>("web-endpoint-status");
                GameServersEndpointStatus = root.Q<Label>("gs-endpoint-status");
                LoggedAs = root.Q<Label>("logged-as");
                AccountGamesSection = root.Q<VisualElement>("account-games-section");
                AccountGamesContainer = root.Q<VisualElement>("account-games-container");
                RegionsSection = root.Q<VisualElement>("regions-section");
                RegionsInfo = root.Q<Label>("regions-info");
                RegionsContainer = root.Q<VisualElement>("regions-container");
                NoGameConfigSection = root.Q<VisualElement>("no-game-config-section");
                ImportGamesButton = root.Q<Button>("import-games-button");
                GameConfigSection = root.Q<VisualElement>("game-config-section");
                GameConfigHeader = root.Q<Label>("game-config-header");
                GameConfigRoot = root.Q<VisualElement>("game-config-root");
                ManageGameHeader = root.Q<Label>("manage-game-header");
                AvailableGamesList = root.Q<ListView>("available-games");
                CreateFirstConfigButton = root.Q<Button>("create-first-config-button");
                LoginUsername = root.Q<TextField>("login-username");
                LoginPassword = root.Q<TextField>("login-password");
                LoginButton = root.Q<Button>("login-button");
                LogoutButton = root.Q<Button>("logout-button");
                LoginWebEndpoint = root.Q<TextField>("login-web-endpoint");
                WebEndpoint = root.Q<TextField>("web-endpoint");
                GameServersEndpoint = root.Q<TextField>("gs-endpoint");
                SynchronizeButton = root.Q<Button>("synchronize-button");
                ClientVersion = root.Q<TextField>("client-version");
                BuildPath = root.Q<TextField>("client-build-path");
                BuildUploadServerButton = root.Q<Button>("build-upload-server-button");
                LogVersionsButton = root.Q<Button>("log-versions-button");
                UploadClientButton = root.Q<Button>("upload-client-button");
                StreamingAssetsLayoutField = root.Q<DropdownField>("sa-layout");
                StreamingAssetsVersionField = root.Q<TextField>("sa-version");
                StreamingAssetsPathField = root.Q<TextField>("sa-path");
                UploadStreamingAssetsButton = root.Q<Button>("upload-sa-button");
            }
        }

        private VisualElements? _elements;

        private static bool isCreatingWindow;

        public static ManageGamesInElympicsWindow ShowWindow(SerializedObject elympicsConfig)
        {
            ManageGamesInElympicsWindow window;
            isCreatingWindow = true;
            try
            {
                window = CreateAndSetUpWindow(elympicsConfig);
            }
            finally
            {
                isCreatingWindow = false;
            }

            window.Rebuild(true);
            return window;
        }

        private static ManageGamesInElympicsWindow CreateAndSetUpWindow(SerializedObject elympicsConfig)
        {
            var initialSize = DefaultWindowSize;
            var usableHeight = EditorGUIUtility.GetMainWindowPosition().height * 0.9f;
            initialSize.y = Mathf.Min(initialSize.y, usableHeight);
            var maxSize = MaxWindowSize;
            maxSize.y = Mathf.Min(maxSize.y, usableHeight);

            var window = GetWindowWithRect<ManageGamesInElympicsWindow>(new Rect(Vector2.zero, initialSize), false, WindowTitle);
            window.minSize = MinWindowSize;
            window.maxSize = maxSize;
            var config = (ElympicsConfig)elympicsConfig.targetObject;
            window.config = config;
            window.clientBuildPath = EditorPrefs.GetString(ClientBuildPathKey, string.Empty);
            window.streamingAssetsPath = EditorPrefs.GetString(StreamingAssetsPathKey, string.Empty);
            window.streamingAssetsVersion = EditorPrefs.GetString(StreamingAssetsVersionKey, string.Empty);
            var storedLayout = EditorPrefs.GetInt(StreamingAssetsLayoutKey, (int)StreamingAssetsLayout.AddressableVariants);
            window.streamingAssetsLayout = Enum.IsDefined(typeof(StreamingAssetsLayout), storedLayout) ? (StreamingAssetsLayout)storedLayout : StreamingAssetsLayout.AddressableVariants;

            var gameConfig = config.GetCurrentGameConfig();
            window.clientVersionName = gameConfig != null ? gameConfig.GameVersion : string.Empty;

            return window;
        }

        private void CreateGUI()
        {
            if (isCreatingWindow)
                return;
            Rebuild();
        }

        private void OnDisable() => DestroyGameConfigEditor();

        private void Rebuild(bool force = false)
        {
            if (windowUxml == null)
                return;

            if (!force && _lastRebuildConfig != null && _lastRebuildConfig == config && rootVisualElement.childCount > 0)
                return;

            _tick?.Pause();
            rootVisualElement.Unbind();
            rootVisualElement.Clear();
            DestroyGameConfigEditor();
            _lastIsLogin = null;
            _lastWebIndicator = null;
            _lastGameServersIndicator = null;

            windowUxml.CloneTree(rootVisualElement);

            if (config == null)
                config = ElympicsConfig.Load();

            var noConfigInfo = rootVisualElement.Q<HelpBox>("no-config-info");
            if (config == null)
            {
                noConfigInfo.SetVisible(true);
                return;
            }

            _lastRebuildConfig = config;
            _serializedConfig = new SerializedConfig(new SerializedObject(config));

            _webEndpointChecker = new EditorEndpointChecker();
            _gameServersEndpointChecker = new EditorEndpointChecker();

            _elements = new VisualElements(rootVisualElement);
            BindLoginSection(_elements, _webEndpointChecker);
            BindEndpointsSection(_elements, _serializedConfig, _webEndpointChecker, _gameServersEndpointChecker);
            BindAvailableGamesSection(_elements);
            BindGameManagementSection(_elements);

            rootVisualElement.Bind(_serializedConfig.Config);

            UpdateLoginState();
            UpdateAccountGames();
            UpdateAvailableRegions();
            UpdateChosenGameConfig();

            // Better alternative to EditorApplication.update
            _tick = rootVisualElement.schedule.Execute(Tick).Every(TickIntervalMs);

            void Tick()
            {
                UpdateEndpointCheckers();
                UpdateLoginState();
            }
        }

        #region Ticking


        private void UpdateEndpointCheckers()
        {
            if (_webEndpointChecker is null || _gameServersEndpointChecker is null || _elements is null)
                return;

            _webEndpointChecker.Update();
            _gameServersEndpointChecker.Update();

            var webIndicator = GetEndpointIndicator(_webEndpointChecker);
            if (_lastWebIndicator != webIndicator)
            {
                _lastWebIndicator = webIndicator;
                SetEndpointStatus(_elements.LoginWebEndpointStatus, webIndicator);
                SetEndpointStatus(_elements.WebEndpointStatus, webIndicator);
            }

            var gameServersIndicator = GetEndpointIndicator(_gameServersEndpointChecker);
            if (_lastGameServersIndicator != gameServersIndicator)
            {
                _lastGameServersIndicator = gameServersIndicator;
                SetEndpointStatus(_elements.GameServersEndpointStatus, gameServersIndicator);
            }
        }

        private static void SetEndpointStatus(Label status, (string Text, Color Color) indicator)
        {
            status.text = indicator.Text;
            status.style.color = indicator.Color;
        }

        private static (string Text, Color Color) GetEndpointIndicator(EditorEndpointChecker checker)
        {
            if (!checker.IsUriCorrect)
                return ("Wrong uri", WrongUriColor);
            if (!checker.IsRequestDone)
                return ("Connecting...", ConnectingColor);
            return checker.IsRequestSuccessful
                ? ("Connected!", ConnectedColor)
                : ("Didn't connect", NotConnectedColor);
        }

        private void UpdateLoginState()
        {
            if (_elements is null)
                return;

            var isLogin = ElympicsConfig.IsLogin;
            if (_lastIsLogin == isLogin)
                return;
            _lastIsLogin = isLogin;

            _elements.LoginView.SetVisible(!isLogin);
            _elements.ManageView.SetVisible(isLogin);
            _elements.LoggedAs.text = ElympicsConfig.Username;
        }

        #endregion

        #region Login Section

        private static void BindLoginSection(VisualElements elements, EditorEndpointChecker webChecker)
        {
            var username = elements.LoginUsername;
            var password = elements.LoginPassword;

            BindToExternalValue(username, ElympicsConfig.Username, value => ElympicsConfig.Username = value);

            password.isPasswordField = true;
            BindToExternalValue(password, ElympicsConfig.Password, value => ElympicsConfig.Password = value);

            elements.LoginButton.clicked += () =>
            {
                if (!IsConnected(webChecker))
                    return;
                ElympicsWebIntegration.Login();
            };

            elements.LogoutButton.clicked += ElympicsWebIntegration.Logout;
        }

        #endregion

        #region Elympics Endpoints Section

        private void BindEndpointsSection(VisualElements elements, SerializedConfig serializedConfig, EditorEndpointChecker webChecker, EditorEndpointChecker gsChecker)
        {
            _ = elements.LoginWebEndpoint.RegisterValueChangedCallback(evt => webChecker.UpdateUri(evt.newValue));
            _ = elements.WebEndpoint.RegisterValueChangedCallback(evt => webChecker.UpdateUri(evt.newValue));
            _ = elements.GameServersEndpoint.RegisterValueChangedCallback(evt => gsChecker.UpdateUri(evt.newValue));

            webChecker.UpdateUri(serializedConfig.ElympicsWebEndpoint.stringValue);
            gsChecker.UpdateUri(serializedConfig.ElympicsGameServersEndpoint.stringValue);

            elements.SynchronizeButton.clicked += Synchronize;
        }

        private void Synchronize()
        {
            if (_webEndpointChecker is null || !IsConnected(_webEndpointChecker))
                return;

            ElympicsWebIntegration.GetElympicsEndpoints(endpoint =>
            {
                if (_gameServersEndpointChecker is null || _serializedConfig is null || config is null)
                    return;

                _serializedConfig.ElympicsGameServersEndpoint.stringValue = endpoint.GameServers;
                _serializedConfig.ApplyModifiedProperties();
                _gameServersEndpointChecker.UpdateUri(endpoint.GameServers);

                ElympicsWebIntegration.GetGames(availableGamesOnline =>
                {
                    ElympicsLogger.LogInfo($"Received {availableGamesOnline.Count} games: {string.Join(", ", availableGamesOnline.Select(x => x.Name))}");
                    _accountGames = availableGamesOnline;
                    UpdateAccountGames();
                });

                var gameId = config.GetCurrentGameConfig()?.GameId;
                if (gameId == null)
                    return;
                ElympicsWebIntegration.GetAvailableRegionsForGameId(gameId,
                    regionsResponse =>
                    {
                        _availableRegions = regionsResponse.Select(x => x.Name).ToArray();
                        ElympicsLogger.LogInfo($"Received {regionsResponse.Count} regions: {string.Join(", ", _availableRegions)}");
                        UpdateAvailableRegions();
                    },
                    () =>
                    {
                        _availableRegions = Array.Empty<string>();
                        ElympicsLogger.LogError($"Error receiving regions for game ID: {gameId}");
                        UpdateAvailableRegions();
                    });
            });
        }

        private void UpdateAccountGames()
        {
            if (_elements is null)
                return;

            _elements.AccountGamesContainer.Clear();
            if (_accountGames.Count == 0)
            {
                _elements.AccountGamesSection.SetVisible(false);
                return;
            }

            _elements.AccountGamesSection.SetVisible(true);
            foreach (var game in _accountGames)
            {
                var row = new VisualElement();
                row.AddToClassList(AccountGameRowClass);
                row.Add(CreateReadOnlyField(game.Name, AccountGameNameClass));
                row.Add(CreateReadOnlyField(game.Id, AccountGameIdClass));
                _elements.AccountGamesContainer.Add(row);
            }
        }

        private void UpdateAvailableRegions()
        {
            if (_elements is null)
                return;

            _elements.RegionsContainer.Clear();
            if (_availableRegions == null)
            {
                _elements.RegionsInfo.text = NotSyncedYetText;
                return;
            }

            if (_availableRegions.Length == 0)
            {
                _elements.RegionsInfo.text = NoRegionsAvailableText;
                return;
            }

            _elements.RegionsInfo.text = RegionsAvailableText;
            foreach (var region in _availableRegions.Where(region => !string.IsNullOrEmpty(region)))
                _elements.RegionsContainer.Add(CreateReadOnlyField(region));
        }

        private static TextField CreateReadOnlyField(string text, string? styleClass = null)
        {
            var field = new TextField { value = text, isReadOnly = true };
            if (styleClass != null)
                field.AddToClassList(styleClass);
            return field;
        }

        #endregion

        #region Available Games Section

        private void BindAvailableGamesSection(VisualElements elements)
        {
            elements.AvailableGamesList.RegisterCallback<ChangeEvent<Object>>(_ => UpdateChosenGameConfig());
            elements.CreateFirstConfigButton.clicked += CreateFirstGameConfig;
            elements.ImportGamesButton.clicked += ImportExistingGameConfigs;
        }

        private void CreateFirstGameConfig()
        {
            if (_serializedConfig is null)
                return;

            var gameConfig = CreateInstance<ElympicsGameConfig>();
            ElympicsTools.EnsureElympicsResourcesDirectoryExists();

            AssetDatabase.CreateAsset(gameConfig, ElympicsConfig.ElympicsResourcesPath + "/ElympicsGameConfig.asset");
            AssetDatabase.SaveAssets();

            AppendGameConfig(_serializedConfig.AvailableGames, gameConfig);
            _serializedConfig.ApplyModifiedProperties();
            UpdateChosenGameConfig();
        }

        private static string[] FindGameConfigGuids() => AssetDatabase.FindAssets($"t:{nameof(ElympicsGameConfig)}");

        private void ImportExistingGameConfigs()
        {
            if (_serializedConfig is null)
                return;

            var configs = FindGameConfigGuids();
            if (configs.Length <= 0)
            {
                ElympicsLogger.LogWarning($"No {nameof(ElympicsGameConfig)} found in assets");
                return;
            }

            foreach (var configGuid in configs)
                AppendGameConfig(_serializedConfig.AvailableGames, AssetDatabase.LoadAssetAtPath<ElympicsGameConfig>(AssetDatabase.GUIDToAssetPath(configGuid)));
            _serializedConfig.ApplyModifiedProperties();
            UpdateChosenGameConfig();
        }

        private void AppendGameConfig(SerializedProperty availableGames, Object gameConfig)
        {
            availableGames.InsertArrayElementAtIndex(availableGames.arraySize);
            availableGames.GetArrayElementAtIndex(availableGames.arraySize - 1).objectReferenceValue = gameConfig;
        }

        private void UpdateChosenGameConfig()
        {
            if (_elements is null || config is null)
                return;

            var gameConfig = config.GetCurrentGameConfig();
            var hasGameConfig = gameConfig != null;
            _elements.NoGameConfigSection.SetVisible(!hasGameConfig);
            _elements.GameConfigSection.SetVisible(hasGameConfig);
            _elements.RegionsSection.SetVisible(hasGameConfig);

            if (gameConfig == null)
            {
                //The import button is only visible in this branch, so the project-wide search is only worth running here
                _elements.ImportGamesButton.text = $"Find and import games ({FindGameConfigGuids().Length})";
                DestroyGameConfigEditor();
                _elements.GameConfigRoot.Clear();
                return;
            }

            UpdateGameHeaders(gameConfig.GameName);

            if (_gameConfigEditor != null && _gameConfigEditor.target == gameConfig)
                return;

            DestroyGameConfigEditor();
            _elements.GameConfigRoot.Clear();

            _gameConfigEditor = UnityEditor.Editor.CreateEditor(gameConfig);
            var inspector = _gameConfigEditor.CreateInspectorGUI();
            inspector.Bind(_gameConfigEditor.serializedObject);
            _elements.GameConfigRoot.Add(inspector);

            var gameName = inspector.Q<TextField>("game-name");
            if (gameName != null)
                _ = gameName.RegisterValueChangedCallback(evt => UpdateGameHeaders(evt.newValue));
        }

        private void UpdateGameHeaders(string gameName)
        {
            if (_elements is null)
                return;

            _elements.GameConfigHeader.text = $"{gameName} settings";
            _elements.ManageGameHeader.text = $"Manage {gameName} in Elympics";
        }

        private void DestroyGameConfigEditor()
        {
            if (_gameConfigEditor != null)
                DestroyImmediate(_gameConfigEditor);
            _gameConfigEditor = null;
        }

        #endregion

        #region Game Management in Elympics Section

        private void BindGameManagementSection(VisualElements elements)
        {
            BindToExternalValue(elements.ClientVersion, clientVersionName, value => clientVersionName = value);
            BindToExternalValue(elements.BuildPath, clientBuildPath, value => clientBuildPath = value);
            BindToExternalValue(elements.StreamingAssetsVersionField, streamingAssetsVersion, value => streamingAssetsVersion = value);
            BindToExternalValue(elements.StreamingAssetsPathField, streamingAssetsPath, value => streamingAssetsPath = value);
            BindLayoutDropdown(elements.StreamingAssetsLayoutField);

            elements.BuildUploadServerButton.clicked += () =>
            {
                if (!ElympicsWebIntegration.IsConnectedToElympics())
                    return;
                ElympicsWebIntegration.BuildAndUploadGame();
            };

            elements.LogVersionsButton.clicked += LogUploadedServerVersions;
            elements.UploadClientButton.clicked += UploadClientBuild;
            elements.UploadStreamingAssetsButton.clicked += UploadStreamingAssets;
        }

        private static string LayoutLabel(StreamingAssetsLayout layout) => layout switch
        {
            StreamingAssetsLayout.AddressableVariants => "Directory of Addressable-based variants",
            StreamingAssetsLayout.UnstructuredAssets => "Unstructured assets",
            _ => layout.ToString(),
        };

        // Choices are set here because the UXML "choices" attribute is unreliable on 2021.3.
        private void BindLayoutDropdown(DropdownField field)
        {
            field.choices = StreamingAssetsLayouts.Select(LayoutLabel).ToList();
            field.SetValueWithoutNotify(LayoutLabel(streamingAssetsLayout));
            _ = field.RegisterValueChangedCallback(evt =>
            {
                var index = field.choices.IndexOf(evt.newValue);
                if (index >= 0)
                    streamingAssetsLayout = StreamingAssetsLayouts[index];
            });
        }

        private void LogUploadedServerVersions()
        {
            if (config is null)
                return;

            var activeGameConfig = config.GetCurrentGameConfig();
            if (activeGameConfig == null || !ElympicsWebIntegration.IsConnectedToElympics())
                return;

            ElympicsWebIntegration.GetGameVersionsForGameId(activeGameConfig.GameId,
                gameVersions =>
                {
                    var log = new StringBuilder();
                    _ = log.AppendFormat("{0,-15} {1,-20}\n", "Game Version", "Upload Time");
                    foreach (var gameVersion in gameVersions.Versions)
                        _ = log.AppendFormat("{0,-15} {1,40}\n", gameVersion.Version, gameVersion.UploadedTime);

                    ElympicsLogger.LogInfo(log.ToString());
                });
        }

        private void UploadClientBuild()
        {
            if (config is null)
                return;

            var activeGameConfig = config.GetCurrentGameConfig();
            if (activeGameConfig == null)
                return;

            EditorPrefs.SetString(ClientBuildPathKey, clientBuildPath);

            if (!ElympicsWebIntegration.IsConnectedToElympics())
                return;

            ElympicsWebIntegration.UploadClientBuild(clientBuildPath, activeGameConfig.GameId, clientVersionName, activeGameConfig.GameVersion);
        }

        private void UploadStreamingAssets()
        {
            if (config is null)
                return;

            var activeGameConfig = config.GetCurrentGameConfig();
            if (activeGameConfig == null)
                return;

            EditorPrefs.SetString(StreamingAssetsPathKey, streamingAssetsPath);
            EditorPrefs.SetString(StreamingAssetsVersionKey, streamingAssetsVersion);
            EditorPrefs.SetInt(StreamingAssetsLayoutKey, (int)streamingAssetsLayout);

            if (!ElympicsWebIntegration.IsConnectedToElympics())
                return;

            ElympicsWebIntegration.UploadStreamingAssets(activeGameConfig.GameId, streamingAssetsPath, streamingAssetsVersion, streamingAssetsLayout);
        }

        #endregion

        private static void BindToExternalValue(TextField field, string? initialValue, Action<string> setValue)
        {
            field.SetValueWithoutNotify(initialValue ?? "");
            _ = field.RegisterValueChangedCallback(evt => setValue(evt.newValue));
        }

        private static bool IsConnected(EditorEndpointChecker webChecker)
        {
            if (webChecker is { IsUriCorrect: true, IsRequestSuccessful: true })
                return true;
            ElympicsLogger.LogError("Cannot connect to Elympics cloud! Check your Internet connection and configured Elympics endpoints.");
            return false;
        }
    }
}
