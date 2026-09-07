using System;
using System.Collections.Generic;
using System.IO;
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
        private const string StreamingAssetsCatalogKey = "ElympicsStreamingAssetsCatalogForUpload";

        [SerializeField] private string? clientBuildPath;
        [SerializeField] private string? streamingAssetCatalogUrl;
        [SerializeField] private string? clientVersionName;

        #endregion

        private const int TickIntervalMs = 200;

        private static readonly Vector2 DefaultWindowSize = new(500, 900);
        private static readonly Vector2 MinWindowSize = new(250, 500);
        private static readonly Vector2 MaxWindowSize = new(1000, 1800);

        private static readonly Color WrongUriColor = new(0.925f, 0.780f, 0.047f);   // #ECC70C
        private static readonly Color ConnectingColor = new(0.000f, 0.380f, 0.882f); // #0061E1
        private static readonly Color ConnectedColor = new(0.388f, 0.871f, 0.294f);  // #63DE4B
        private static readonly Color NotConnectedColor = new(0.882f, 0.000f, 0.118f); // #E1001E

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

        private (EditorEndpointChecker Web, EditorEndpointChecker GameServers)? _endpointCheckers;

        private string[]? _availableRegions;
        private List<ElympicsWebIntegration.GameResponseModel> _accountGames = new();

        private UnityEditor.Editor? _gameConfigEditor;
        private bool? _lastIsLogin;

        // NOT SERIALIZED for rebuilding the visual tree after a domain reload.
        private ElympicsConfig? _lastRebuildConfig;

        private class VisualElements
        {
            public VisualElement LoginView { get; init; } = null!;
            public VisualElement ManageView { get; init; } = null!;
            public Label LoginWebEndpointStatus { get; init; } = null!;
            public Label WebEndpointStatus { get; init; } = null!;
            public Label GameServersEndpointStatus { get; init; } = null!;
            public Label LoggedAs { get; init; } = null!;
            public VisualElement AccountGamesSection { get; init; } = null!;
            public VisualElement AccountGamesContainer { get; init; } = null!;
            public VisualElement RegionsSection { get; init; } = null!;
            public Label RegionsInfo { get; init; } = null!;
            public VisualElement RegionsContainer { get; init; } = null!;
            public VisualElement NoGameConfigSection { get; init; } = null!;
            public Button ImportGamesButton { get; init; } = null!;
            public VisualElement GameConfigSection { get; init; } = null!;
            public Label GameConfigHeader { get; init; } = null!;
            public VisualElement GameConfigRoot { get; init; } = null!;
            public Label ManageGameHeader { get; init; } = null!;
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
            window.streamingAssetCatalogUrl = EditorPrefs.GetString(StreamingAssetsCatalogKey, string.Empty);

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

            windowUxml.CloneTree(rootVisualElement);

            if (config == null)
                config = ElympicsConfig.Load();

            var noConfigInfo = rootVisualElement.Q<HelpBox>("no-config-info");
            if (config == null)
            {
                SetVisible(noConfigInfo, true);
                return;
            }

            _lastRebuildConfig = config;
            _serializedConfig = new SerializedConfig(new SerializedObject(config));

            _endpointCheckers = (new EditorEndpointChecker(), new EditorEndpointChecker());

            QueryElements();
            BindLoginSection();
            BindEndpointsSection();
            BindAvailableGamesSection();
            BindGameManagementSection();

            rootVisualElement.Bind(_serializedConfig.Config);

            UpdateLoginState();
            UpdateAccountGames();
            UpdateAvailableRegions();
            UpdateChosenGameConfig();

            _tick = rootVisualElement.schedule.Execute(Tick).Every(TickIntervalMs);
        }

        private void QueryElements()
        {
            _elements = new VisualElements
            {
                LoginView = rootVisualElement.Q<VisualElement>("login-view"),
                ManageView = rootVisualElement.Q<VisualElement>("manage-view"),
                LoginWebEndpointStatus = rootVisualElement.Q<Label>("login-web-endpoint-status"),
                WebEndpointStatus = rootVisualElement.Q<Label>("web-endpoint-status"),
                GameServersEndpointStatus = rootVisualElement.Q<Label>("gs-endpoint-status"),
                LoggedAs = rootVisualElement.Q<Label>("logged-as"),
                AccountGamesSection = rootVisualElement.Q<VisualElement>("account-games-section"),
                AccountGamesContainer = rootVisualElement.Q<VisualElement>("account-games-container"),
                RegionsSection = rootVisualElement.Q<VisualElement>("regions-section"),
                RegionsInfo = rootVisualElement.Q<Label>("regions-info"),
                RegionsContainer = rootVisualElement.Q<VisualElement>("regions-container"),
                NoGameConfigSection = rootVisualElement.Q<VisualElement>("no-game-config-section"),
                ImportGamesButton = rootVisualElement.Q<Button>("import-games-button"),
                GameConfigSection = rootVisualElement.Q<VisualElement>("game-config-section"),
                GameConfigHeader = rootVisualElement.Q<Label>("game-config-header"),
                GameConfigRoot = rootVisualElement.Q<VisualElement>("game-config-root"),
                ManageGameHeader = rootVisualElement.Q<Label>("manage-game-header"),
            };
        }

        #region Ticking

        //IMGUI used to drive the endpoint checkers on every repaint. The scheduler is bound to the window's panel,
        //so it stops on its own when the window is closed - unlike EditorApplication.update, which would need manual cleanup.
        private void Tick()
        {
            UpdateEndpointCheckers();
            UpdateLoginState();
        }

        private void UpdateEndpointCheckers()
        {
            if (_endpointCheckers is null || _elements is null)
                return;

            _endpointCheckers.Value.Web.Update();
            _endpointCheckers.Value.GameServers.Update();

            //Both endpoint labels show the same checker - the login and manage views are mutually exclusive
            var webIndicator = GetEndpointIndicator(_endpointCheckers.Value.Web);
            SetEndpointStatus(_elements.LoginWebEndpointStatus, webIndicator);
            SetEndpointStatus(_elements.WebEndpointStatus, webIndicator);
            SetEndpointStatus(_elements.GameServersEndpointStatus, GetEndpointIndicator(_endpointCheckers.Value.GameServers));
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

        //ElympicsConfig.IsLogin is backed by EditorPrefs and exposes no change event, so it has to be polled
        private void UpdateLoginState()
        {
            if (_elements is null)
                return;

            var isLogin = ElympicsConfig.IsLogin;
            if (_lastIsLogin == isLogin)
                return;
            _lastIsLogin = isLogin;

            SetVisible(_elements.LoginView, !isLogin);
            SetVisible(_elements.ManageView, isLogin);
            _elements.LoggedAs.text = ElympicsConfig.Username;
        }

        #endregion

        #region Login Section

        private void BindLoginSection()
        {
            var username = rootVisualElement.Q<TextField>("login-username");
            var password = rootVisualElement.Q<TextField>("login-password");

            BindToExternalValue(username, ElympicsConfig.Username, value => ElympicsConfig.Username = value);

            password.isPasswordField = true;
            BindToExternalValue(password, ElympicsConfig.Password, value => ElympicsConfig.Password = value);

            rootVisualElement.Q<Button>("login-button").clicked += () =>
            {
                if (!IsConnected())
                    return;
                ElympicsWebIntegration.Login();
            };

            rootVisualElement.Q<Button>("logout-button").clicked += ElympicsWebIntegration.Logout;
        }

        #endregion

        #region Elympics Endpoints Section

        private void BindEndpointsSection()
        {
            if (_endpointCheckers is null)
                throw new InvalidOperationException("Could not bind endpoints section due to endpoint checkers being null");

            var loginWebEndpoint = rootVisualElement.Q<TextField>("login-web-endpoint");
            var webEndpoint = rootVisualElement.Q<TextField>("web-endpoint");
            var gameServersEndpoint = rootVisualElement.Q<TextField>("gs-endpoint");

            //Both fields are bound to the same property and share a single checker, which ignores repeated URIs
            _ = loginWebEndpoint.RegisterValueChangedCallback(evt => _endpointCheckers?.Web.UpdateUri(evt.newValue));
            _ = webEndpoint.RegisterValueChangedCallback(evt => _endpointCheckers?.Web.UpdateUri(evt.newValue));
            _ = gameServersEndpoint.RegisterValueChangedCallback(evt => _endpointCheckers?.GameServers.UpdateUri(evt.newValue));

            _endpointCheckers.Value.Web.UpdateUri(_serializedConfig?.ElympicsWebEndpoint.stringValue);
            _endpointCheckers.Value.GameServers.UpdateUri(_serializedConfig?.ElympicsGameServersEndpoint.stringValue);

            rootVisualElement.Q<Button>("synchronize-button").clicked += Synchronize;
        }

        private void Synchronize()
        {
            if (!IsConnected())
                return;

            ElympicsWebIntegration.GetElympicsEndpoints(endpoint =>
            {
                if (_endpointCheckers is null || _serializedConfig is null || config is null)
                    return;

                _serializedConfig.ElympicsGameServersEndpoint.stringValue = endpoint.GameServers;
                _serializedConfig.ApplyModifiedProperties();
                _endpointCheckers.Value.GameServers.UpdateUri(endpoint.GameServers);

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
                SetVisible(_elements.AccountGamesSection, false);
                return;
            }

            SetVisible(_elements.AccountGamesSection, true);
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

        private void BindAvailableGamesSection()
        {
            if (_elements is null)
                return;

            rootVisualElement.Q<ListView>("available-games").RegisterCallback<ChangeEvent<Object>>(_ => UpdateChosenGameConfig());

            rootVisualElement.Q<Button>("create-first-config-button").clicked += CreateFirstGameConfig;
            _elements.ImportGamesButton.clicked += ImportExistingGameConfigs;
        }

        private void CreateFirstGameConfig()
        {
            if (_serializedConfig is null)
                return;

            var gameConfig = CreateInstance<ElympicsGameConfig>();
            if (!Directory.Exists(ElympicsConfig.ElympicsResourcesPath))
            {
                ElympicsLogger.LogInfo("Creating Elympics Resources directory...");
                _ = Directory.CreateDirectory(ElympicsConfig.ElympicsResourcesPath);
                ElympicsLogger.LogInfo("Elympics Resources directory created successfully.");
            }

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
            SetVisible(_elements.NoGameConfigSection, !hasGameConfig);
            SetVisible(_elements.GameConfigSection, hasGameConfig);
            SetVisible(_elements.RegionsSection, hasGameConfig);

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

        private void BindGameManagementSection()
        {
            var clientVersion = rootVisualElement.Q<TextField>("client-version");
            var buildPath = rootVisualElement.Q<TextField>("client-build-path");

            BindToExternalValue(clientVersion, clientVersionName, value => clientVersionName = value);
            BindToExternalValue(buildPath, clientBuildPath, value => clientBuildPath = value);

            rootVisualElement.Q<Button>("build-upload-server-button").clicked += () =>
            {
                if (!ElympicsWebIntegration.IsConnectedToElympics())
                    return;
                ElympicsWebIntegration.BuildAndUploadGame();
            };

            rootVisualElement.Q<Button>("log-versions-button").clicked += LogUploadedServerVersions;
            rootVisualElement.Q<Button>("upload-client-button").clicked += UploadClientBuild;
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

                    Debug.Log(log.ToString());
                });
        }

        private void UploadClientBuild()
        {
            if (config is null)
                return;

            var activeGameConfig = config.GetCurrentGameConfig();
            if (activeGameConfig == null)
                return;

            //Save last used paths in editor prefs, so they can persist editor restarts
            EditorPrefs.SetString(ClientBuildPathKey, clientBuildPath);
            EditorPrefs.SetString(StreamingAssetsCatalogKey, streamingAssetCatalogUrl);

            if (!ElympicsWebIntegration.IsConnectedToElympics())
                return;

            ElympicsWebIntegration.UploadClientBuild(clientBuildPath, activeGameConfig.GameId, clientVersionName, activeGameConfig.GameVersion, streamingAssetCatalogUrl);
        }

        #endregion

        private static void SetVisible(VisualElement element, bool visible) => element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        private static void BindToExternalValue(TextField field, string? initialValue, Action<string> setValue)
        {
            field.SetValueWithoutNotify(initialValue ?? "");
            _ = field.RegisterValueChangedCallback(evt => setValue(evt.newValue));
        }

        private bool IsConnected()
        {
            if (_endpointCheckers != null && _endpointCheckers.Value.Web.IsUriCorrect && _endpointCheckers.Value.Web.IsRequestSuccessful)
                return true;
            ElympicsLogger.LogError("Cannot connect to Elympics cloud! " + "Check your Internet connection and configured Elympics endpoints.");
            return false;
        }
    }
}
