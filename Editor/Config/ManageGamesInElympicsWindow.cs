using System.Collections.Generic;
using System.IO;
using System.Linq;
using Elympics;
using UnityEditor;
using UnityEngine;

public class ManageGamesInElympicsWindow : EditorWindow
{
    #region Labels

    private const string WindowTitle = "Manage games in Elympics";
    private const string LoginHeaderInfo = "<i>You have to be logged in to manage games in Elympics!</i>";
    private const string LoggedAsInfo = "Logged in <color=#2EACFF>ElympicsWeb</color> as ";

    private const string SynchronizeInfo = "Synchronize endpoints and games available for your account";

    private const string NoGameSetupsInfo = "<i>You don't have any available games yet. Click button below to create first Elympics Config!</i>";

    private const string UploadGameInfo = "Upload new version of game with current settings to Elympics, game name and game id in config should match with game in Elympics. "
        + "It's required to first upload a game version if you want to play it in online mode.";

    private const string ImportExistingsGamesInfo = "<i>or import existing games (e.g. samples)</i>";

    #endregion

    #region Colors

    private bool colorsConverted = false;
    private const string ElympicsColorHex = "#2EACFF";
    private Color elympicsColor = Color.blue;

    #endregion

    #region Data Received From Elympics Config

    private static SerializedObject elympicsConfigSerializedObject;
    private static SerializedProperty currentGameIndex;
    private static SerializedProperty availableGames;
    private static SerializedProperty elympicsWebEndpoint;
    private static EditorEndpointChecker elympicsWebEndpointChecker;
    private static SerializedProperty elympicsGameServersEndpoint;
    private static EditorEndpointChecker elympicsGameServersEndpointChecker;

    #endregion

    private List<string> _availableRegions;
    private List<ElympicsWebIntegration.GameResponseModel> _accountGames;
    private CustomInspectorDrawer _customInspectorDrawer;
    private ElympicsGameConfigGeneralInfoDrawer _elympicsGameConfigInfoDrawer;
    private GUIStyle _guiStyleWrappedTextCalculator;

    private int _resizableCenteredLabelWidth;

    private static ManageGamesInElympicsWindowData manageGamesInElympicsWindowData;

    public static ManageGamesInElympicsWindow ShowWindow(
        SerializedObject elympicsConfigSerializedObject,
        SerializedProperty currentGameIndex,
        SerializedProperty availableGames,
        SerializedProperty elympicsWebEndpoint,
        SerializedProperty elympicsGameServersEndpoint)
    {
        ManageGamesInElympicsWindow.elympicsConfigSerializedObject = elympicsConfigSerializedObject;
        ManageGamesInElympicsWindow.currentGameIndex = currentGameIndex;
        ManageGamesInElympicsWindow.availableGames = availableGames;
        ManageGamesInElympicsWindow.elympicsWebEndpoint = elympicsWebEndpoint;
        ManageGamesInElympicsWindow.elympicsGameServersEndpoint = elympicsGameServersEndpoint;

        elympicsWebEndpointChecker = new EditorEndpointChecker();
        elympicsWebEndpointChecker.UpdateUri(elympicsWebEndpoint.stringValue);
        elympicsGameServersEndpointChecker = new EditorEndpointChecker();
        elympicsGameServersEndpointChecker.UpdateUri(elympicsGameServersEndpoint.stringValue);

        manageGamesInElympicsWindowData = Resources.Load<ManageGamesInElympicsWindowData>("ManageGamesInElympicsWindowData");

        SaveDataToScriptableObject();

        var window = GetWindowWithRect<ManageGamesInElympicsWindow>(new Rect(0, 0, 500, 900), false, WindowTitle);
        window.minSize = new Vector2(250, 500);
        window.maxSize = new Vector2(1000, 1800);

        return window;
    }

    private static bool GetManageGamesInElympicsWindowData()
    {
        //Due to window crashes on game upload it's necessary to get object from resources every time
        //Without this editor window will crash when the game is building
        manageGamesInElympicsWindowData = Resources.Load<ManageGamesInElympicsWindowData>("ManageGamesInElympicsWindowData");

        if (manageGamesInElympicsWindowData == null)
            return false;
        if (manageGamesInElympicsWindowData.objectToSerialize == null)
        {
            ElympicsTools.OpenManageGamesInElympicsWindow();
            return false;
        }

        elympicsConfigSerializedObject = new SerializedObject(manageGamesInElympicsWindowData.objectToSerialize);
        currentGameIndex = elympicsConfigSerializedObject.FindProperty("currentGame");
        availableGames = elympicsConfigSerializedObject.FindProperty("availableGames");
        elympicsWebEndpoint = elympicsConfigSerializedObject.FindProperty("elympicsWebEndpoint");
        elympicsGameServersEndpoint = elympicsConfigSerializedObject.FindProperty("elympicsGameServersEndpoint");

        if (elympicsWebEndpointChecker == null
            || elympicsGameServersEndpointChecker == null)
        {
            elympicsWebEndpointChecker = new EditorEndpointChecker();
            elympicsGameServersEndpointChecker = new EditorEndpointChecker();
            elympicsWebEndpointChecker.UpdateUri(elympicsWebEndpoint.stringValue);
            elympicsGameServersEndpointChecker.UpdateUri(elympicsGameServersEndpoint.stringValue);
        }

        return true;
    }

    private static void SaveDataToScriptableObject()
    {
        if (manageGamesInElympicsWindowData == null)
            _ = GetManageGamesInElympicsWindowData();

        manageGamesInElympicsWindowData.objectToSerialize = elympicsConfigSerializedObject.targetObject;
    }

    private void ConvertColorsFromHexToUnity()
    {
        if (ColorUtility.TryParseHtmlString(ElympicsColorHex, out var convertedColor))
            elympicsColor = convertedColor;

        colorsConverted = true;
    }

    private void OnGUI()
    {
        if (BuildPipeline.isBuildingPlayer || !GetManageGamesInElympicsWindowData())
            return;

        elympicsWebEndpointChecker.Update();
        elympicsGameServersEndpointChecker.Update();

        PrepareDrawer();
        PrepareGUIStyleCalculator();

        if (!ElympicsConfig.IsLogin)
            DrawLoginToElympicsContent();
        else
            DrawManageGamesContent();

        _ = elympicsConfigSerializedObject.ApplyModifiedProperties();
    }

    private void PrepareDrawer()
    {
        if (!colorsConverted)
            ConvertColorsFromHexToUnity();

        _customInspectorDrawer ??= new CustomInspectorDrawer(position, 5, 15);
        _customInspectorDrawer.PrepareToDraw(position);

        _resizableCenteredLabelWidth = (int)(position.width * 0.80f);
    }

    private void PrepareGUIStyleCalculator()
    {
        if (_guiStyleWrappedTextCalculator != null)
            return;

        _guiStyleWrappedTextCalculator = new GUIStyle
        {
            richText = true,
            wordWrap = true
        };
    }

    private void DrawManageGamesContent()
    {
        _customInspectorDrawer.Space();

        DrawAccountSection();
        DrawEndpointsSection();
        DrawAvailableGamesSection();
    }

    private void DrawAvailableRegionSection()
    {
        _customInspectorDrawer.Space();
        _customInspectorDrawer.DrawAvailableRegions(_availableRegions);
    }

    private void DrawLoginToElympicsContent()
    {
        _customInspectorDrawer.Space();

        DrawLoginSection();
        DrawLoginEndpointSection();
    }

    #region Elympics Endpoints Section

    private void DrawEndpointsSection()
    {
        _customInspectorDrawer.DrawHeader("Endpoints", 20, elympicsColor);
        _customInspectorDrawer.Space();

        _customInspectorDrawer.DrawEndpoint("Web endpoint", elympicsWebEndpoint, elympicsWebEndpointChecker, 0.3f, 0.3f, out var webEndpointChanged);
        if (webEndpointChanged)
            elympicsWebEndpointChecker.UpdateUri(elympicsWebEndpoint.stringValue);

        if (_customInspectorDrawer.DrawButtonCentered("Synchronize", _resizableCenteredLabelWidth, 20))
        {
            if (!IsConnected())
                return;

            ElympicsWebIntegration.GetElympicsEndpoints(endpoint =>
            {
                elympicsGameServersEndpoint.SetValue(endpoint.GameServers);

                ElympicsWebIntegration.GetGames(availableGamesOnline =>
                {
                    ElympicsLogger.Log($"Received {availableGamesOnline.Count} games: {string.Join(", ", availableGamesOnline.Select(x => x.Name))}");
                    _accountGames = availableGamesOnline;
                });
                var gameId = ((List<ElympicsGameConfig>)availableGames.GetValue())[currentGameIndex.intValue].gameId;
                ElympicsWebIntegration.GetAvailableRegionsForGameId(gameId,
                    regionsResponse =>
                    {
                        _availableRegions = regionsResponse.Select(x => x.Name).ToList();
                        ElympicsLogger.Log($"Received {regionsResponse.Count} regions: {string.Join(", ", _availableRegions)}");
                    },
                    () =>
                    {
                        _availableRegions = new List<string>();
                        ElympicsLogger.LogError($"Error receiving regions for game ID: {gameId}");
                    });
            });
            GUI.FocusControl(null);
        }

        _customInspectorDrawer.DrawLabelCentered(SynchronizeInfo, _resizableCenteredLabelWidth, 20, true);
        _customInspectorDrawer.Space();

        _customInspectorDrawer.DrawEndpoint("Game Servers endpoint", elympicsGameServersEndpoint, elympicsGameServersEndpointChecker, 0.3f, 0.3f, out var gameServersEndpointChanged);
        if (gameServersEndpointChanged)
            elympicsGameServersEndpointChecker.UpdateUri(elympicsGameServersEndpoint.stringValue);

        _customInspectorDrawer.DrawAccountGames(_accountGames);
    }

    #endregion

    #region Available Games Section

    private void DrawAvailableGamesSection()
    {
        _customInspectorDrawer.DrawHeader("Local games configurations", 20, elympicsColor);

        var chosenGameProperty = GetChosenGameProperty();

        if (availableGames.GetValue() == null)
            availableGames.SetValue(new List<ElympicsGameConfig>());

        if (chosenGameProperty != null
            && chosenGameProperty.objectReferenceValue != null)
        {
            currentGameIndex.intValue = _customInspectorDrawer.DrawPopup("Active game:",
                currentGameIndex.intValue,
                ((List<ElympicsGameConfig>)availableGames.GetValue()).Select(x => $"{(x != null ? x.GameName : string.Empty)} ({(x != null ? x.GameId : string.Empty)})").ToArray());
            DrawAvailableRegionSection();
            _customInspectorDrawer.DrawSerializedProperty("Local games configurations", availableGames);
            _customInspectorDrawer.Space();

            PrepareElympicsGameConfigDrawer(chosenGameProperty);

            _elympicsGameConfigInfoDrawer.DrawGeneralGameConfigInfo();

            DrawGameManagementInElympicsSection(chosenGameProperty.objectReferenceValue as ElympicsGameConfig);

            _elympicsGameConfigInfoDrawer.ApplyModifications();
        }
        else
            DrawButtonForCreatingFirstSetup(availableGames, currentGameIndex);

        _ = elympicsConfigSerializedObject.ApplyModifiedProperties();
    }

    private void DrawButtonForCreatingFirstSetup(SerializedProperty availableGames, SerializedProperty currentGameIndex)
    {
        _customInspectorDrawer.DrawLabelCentered(NoGameSetupsInfo, _resizableCenteredLabelWidth, 40, true);

        if (_customInspectorDrawer.DrawButtonCentered("Create first game config!", _resizableCenteredLabelWidth, 20))
        {
            var config = CreateInstance<ElympicsGameConfig>();
            if (!Directory.Exists(ElympicsConfig.ElympicsResourcesPath))
            {
                ElympicsLogger.Log("Creating Elympics Resources directory...");
                _ = Directory.CreateDirectory(ElympicsConfig.ElympicsResourcesPath);
                ElympicsLogger.Log("Elympics Resources directory created successfully.");
            }

            AssetDatabase.CreateAsset(config, ElympicsConfig.ElympicsResourcesPath + "/ElympicsGameConfig.asset");
            AssetDatabase.SaveAssets();
            availableGames.InsertArrayElementAtIndex(availableGames.arraySize);
            var value = availableGames.GetArrayElementAtIndex(availableGames.arraySize - 1);
            value.objectReferenceValue = config;
            currentGameIndex.intValue = availableGames.arraySize - 1;
        }

        var configs = AssetDatabase.FindAssets($"t:{nameof(ElympicsGameConfig)}");
        _customInspectorDrawer.DrawLabelCentered(ImportExistingsGamesInfo, _resizableCenteredLabelWidth, 20, true);

        if (!_customInspectorDrawer.DrawButtonCentered($"Find and import games ({configs.Length})", _resizableCenteredLabelWidth, 20))
            return;

        if (configs.Length <= 0)
        {
            ElympicsLogger.LogWarning($"No {nameof(ElympicsGameConfig)} found in assets");
            return;
        }

        foreach (var config in configs)
        {
            availableGames.InsertArrayElementAtIndex(availableGames.arraySize);
            var value = availableGames.GetArrayElementAtIndex(availableGames.arraySize - 1);
            value.objectReferenceValue = AssetDatabase.LoadAssetAtPath<ElympicsGameConfig>(AssetDatabase.GUIDToAssetPath(config));
        }

        currentGameIndex.intValue = availableGames.arraySize - 1;
    }

    private void PrepareElympicsGameConfigDrawer(SerializedProperty activeGameConfig)
    {
        if (_elympicsGameConfigInfoDrawer == null)
        {
            _elympicsGameConfigInfoDrawer = new ElympicsGameConfigGeneralInfoDrawer(_customInspectorDrawer, elympicsColor);
            _elympicsGameConfigInfoDrawer.DataChanged += () => ProcessElympicsGameConfigDataChanged(activeGameConfig);
        }

        _elympicsGameConfigInfoDrawer.UpdateGameConfigProperty(activeGameConfig);
    }

    private void ProcessElympicsGameConfigDataChanged(SerializedProperty activeGameConfig)
    {
        ((ElympicsGameConfig)activeGameConfig.objectReferenceValue).ProcessElympicsConfigDataChanged();
    }

    private static SerializedProperty GetChosenGameProperty()
    {
        var chosen = currentGameIndex.intValue;
        if (availableGames.arraySize == 0)
            return null;

        if (chosen < 0)
            chosen = 0;
        if (chosen >= availableGames.arraySize)
            chosen = availableGames.arraySize - 1;

        return availableGames.GetArrayElementAtIndex(chosen);
    }

    #endregion

    #region Game Management in Elympics Section

    private void DrawGameManagementInElympicsSection(ElympicsGameConfig activeGameConfig)
    {
        _customInspectorDrawer.DrawHeader("Manage " + activeGameConfig.gameName + " in Elympics", 20, elympicsColor);
        _customInspectorDrawer.Space();

        if (_customInspectorDrawer.DrawButtonCentered("Upload", _resizableCenteredLabelWidth, 20))
        {
            if (!ElympicsWebIntegration.IsConnectedToElympics())
                return;

            ElympicsWebIntegration.BuildAndUploadGame();
            GUIUtility.ExitGUI();
        }
        _customInspectorDrawer.Space();
        if (_customInspectorDrawer.DrawButtonCentered("Log Uploaded Versions", _resizableCenteredLabelWidth, 20))
        {
            if (!ElympicsWebIntegration.IsConnectedToElympics())
                return;
            ElympicsWebIntegration.GetGameVersionsForGameId(activeGameConfig.gameId,
                gameVersions =>
                {
                    var log = string.Format("{0,-15} {1,-20}\n", "Game Version", "Upload Time");
                    foreach (var gameVersion in gameVersions.Versions)
                        log += string.Format("{0,-15} {1,40}\n", gameVersion.Version, gameVersion.UploadedTime);

                    Debug.Log(log);
                });
        }

        var wrappedLabelHeight = (int)_guiStyleWrappedTextCalculator.CalcHeight(new GUIContent(UploadGameInfo), position.width * 0.8f);
        _customInspectorDrawer.DrawLabelCentered(UploadGameInfo, _resizableCenteredLabelWidth, wrappedLabelHeight, true);
    }

    #endregion

    #region Account Section

    private void DrawAccountSection()
    {
        _customInspectorDrawer.DrawHeader("Account", 20, elympicsColor);
        DrawHeaderLoggedAs(ElympicsConfig.Username);
        _customInspectorDrawer.Space();
        DrawLogoutButtonCentered();
        _customInspectorDrawer.Space();
    }

    private void DrawHeaderLoggedAs(string username)
    {
        var content = LoggedAsInfo + $"<b>{username}</b>";

        _customInspectorDrawer.DrawLabelCentered(content, 400, 20, false);
    }

    private void DrawLogoutButtonCentered()
    {
        if (_customInspectorDrawer.DrawButtonCentered("Logout", 150, 30))
        {
            ElympicsWebIntegration.Logout();
            GUI.FocusControl(null);
        }
    }

    #endregion

    #region Login Section

    private void DrawLoginEndpointSection()
    {
        _customInspectorDrawer.DrawHeader("Elympics endpoints", 20, elympicsColor);
        _customInspectorDrawer.Space();

        _customInspectorDrawer.DrawEndpoint("Elympics Web endpoint", elympicsWebEndpoint, elympicsWebEndpointChecker, 0.3f, 0.3f, out var endpointChanged);

        if (endpointChanged)
            elympicsWebEndpointChecker.UpdateUri(elympicsWebEndpoint.stringValue);
    }

    private void DrawLoginSection()
    {
        _customInspectorDrawer.DrawHeader("Account", 20, elympicsColor);
        _customInspectorDrawer.DrawLabelCentered(LoginHeaderInfo, 400, 20, false);

        DrawLoginUsername();
        DrawLoginPassword();

        _customInspectorDrawer.Space();

        DrawLoginButton();
    }

    private void DrawLoginUsername()
    {
        _customInspectorDrawer.DrawLabelCentered("Username", 200, 20, false);
        ElympicsConfig.Username = _customInspectorDrawer.DrawTextFieldCentered(ElympicsConfig.Username, 200, 20);
    }

    private void DrawLoginPassword()
    {
        _customInspectorDrawer.DrawLabelCentered("Password", 200, 20, false);
        ElympicsConfig.Password = _customInspectorDrawer.DrawPasswordFieldCentered(ElympicsConfig.Password, 200, 20);
    }

    private void DrawLoginButton()
    {
        if (_customInspectorDrawer.DrawButtonCentered("Login", 150, 30))
        {
            if (!IsConnected())
                return;
            ElympicsWebIntegration.Login();
        }
    }

    #endregion

    private static bool IsConnected()
    {
        if (elympicsWebEndpointChecker.IsRequestSuccessful)
            return true;
        ElympicsLogger.LogError("Cannot connect to Elympics cloud! " + "Check your Internet connection and configured Elympics endpoints.");
        return false;
    }
}
