using Elympics;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ManageGamesInElympicsWindow : EditorWindow
{
	#region Labels

	private const string windowTitle     = "Manage games in Elympics";
	private const string loginHeaderInfo = "<i>You have to be logged in to manage games in Elympics!</i>";
	private const string loggedAsInfo    = "Logged in <color=#2EACFF>ElympicsWeb</color> as ";

	private const string synchronizeInfo = "Synchronize endpoints and games available for your account";

	private const string noGameSetupsInfo = "<i>You don't have any available games yet. Click button below to create first Elympics Config!</i>";

	private const string uploadGameInfo = "Upload new version of game with current settings to Elympics, game name and game id in config should match with game in Elympics. " +
	                                      "It's required to first upload a game version if you want to play it in online mode.";

	private const string importExistingsGamesInfo = "<i>or import existing games (e.g. samples)</i>";

	#endregion

	#region Colors

	private       bool   colorsConverted  = false;
	private const string elympicsColorHex = "#2EACFF";
	private       Color  elympicsColor    = Color.blue;

	#endregion

	#region Data Received From Elympics Config

	private static SerializedObject      elympicsConfigSerializedObject;
	private static SerializedProperty    currentGameIndex;
	private static SerializedProperty    availableGames;
	private static SerializedProperty    elympicsApiEndpoint;
	private static EditorEndpointChecker elympicsApiEndpointChecker;
	private static SerializedProperty    elympicsLobbyEndpoint;
	private static EditorEndpointChecker elympicsLobbyEndpointChecker;
	private static SerializedProperty    elympicsGameServersEndpoint;
	private static EditorEndpointChecker elympicsGameServersEndpointChecker;

	#endregion

	private List<ElympicsWebIntegration.GameResponseModel> accountGames;
	private CustomInspectorDrawer                          customInspectorDrawer         = null;
	private ElympicsGameConfigGeneralInfoDrawer            elympicsGameConfigInfoDrawer  = null;
	private GUIStyle                                       guiStyleWrappedTextCalculator = null;

	private int resizibleCenteredLabelWidth = 0;

	private static ManageGamesInElympicsWindowData manageGamesInElympicsWindowData = null;

	public static ManageGamesInElympicsWindow ShowWindow(SerializedObject elympicsConfigSerializedObject, SerializedProperty currentGameIndex, SerializedProperty availableGames, SerializedProperty elympicsApiEndpoint,
		SerializedProperty elympicsLobbyEndpoint, SerializedProperty elympicsGameServersEndpoint)
	{
		ManageGamesInElympicsWindow.elympicsConfigSerializedObject = elympicsConfigSerializedObject;
		ManageGamesInElympicsWindow.currentGameIndex = currentGameIndex;
		ManageGamesInElympicsWindow.availableGames = availableGames;
		ManageGamesInElympicsWindow.elympicsApiEndpoint = elympicsApiEndpoint;
		ManageGamesInElympicsWindow.elympicsLobbyEndpoint = elympicsLobbyEndpoint;
		ManageGamesInElympicsWindow.elympicsGameServersEndpoint = elympicsGameServersEndpoint;

		ManageGamesInElympicsWindow.elympicsApiEndpointChecker = new EditorEndpointChecker();
		ManageGamesInElympicsWindow.elympicsApiEndpointChecker.UpdateUri(elympicsApiEndpoint.stringValue);
		ManageGamesInElympicsWindow.elympicsLobbyEndpointChecker = new EditorEndpointChecker();
		ManageGamesInElympicsWindow.elympicsLobbyEndpointChecker.UpdateUri(elympicsLobbyEndpoint.stringValue);
		ManageGamesInElympicsWindow.elympicsGameServersEndpointChecker = new EditorEndpointChecker();
		ManageGamesInElympicsWindow.elympicsGameServersEndpointChecker.UpdateUri(elympicsGameServersEndpoint.stringValue);

		manageGamesInElympicsWindowData = Resources.Load<ManageGamesInElympicsWindowData>("ManageGamesInElympicsWindowData");

		SaveDataToScriptableObject();

		ManageGamesInElympicsWindow window = GetWindowWithRect<ManageGamesInElympicsWindow>(new Rect(0, 0, 500, 900), false, windowTitle);
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

		ManageGamesInElympicsWindow.elympicsConfigSerializedObject = new UnityEditor.SerializedObject(manageGamesInElympicsWindowData.objectToSerialize);
		ManageGamesInElympicsWindow.currentGameIndex = elympicsConfigSerializedObject.FindProperty("currentGame");
		ManageGamesInElympicsWindow.availableGames = elympicsConfigSerializedObject.FindProperty("availableGames");
		ManageGamesInElympicsWindow.elympicsApiEndpoint = elympicsConfigSerializedObject.FindProperty("elympicsApiEndpoint");
		ManageGamesInElympicsWindow.elympicsLobbyEndpoint = elympicsConfigSerializedObject.FindProperty("elympicsLobbyEndpoint");
		ManageGamesInElympicsWindow.elympicsGameServersEndpoint = elympicsConfigSerializedObject.FindProperty("elympicsGameServersEndpoint");

		if (elympicsApiEndpointChecker == null || elympicsLobbyEndpointChecker == null || elympicsGameServersEndpointChecker == null)
		{
			elympicsApiEndpointChecker = new EditorEndpointChecker();
			elympicsLobbyEndpointChecker = new EditorEndpointChecker();
			elympicsGameServersEndpointChecker = new EditorEndpointChecker();
			elympicsApiEndpointChecker.UpdateUri(elympicsApiEndpoint.stringValue);
			elympicsLobbyEndpointChecker.UpdateUri(elympicsLobbyEndpoint.stringValue);
			elympicsGameServersEndpointChecker.UpdateUri(elympicsGameServersEndpoint.stringValue);
		}

		return true;
	}

	private static void SaveDataToScriptableObject()
	{
		if (manageGamesInElympicsWindowData == null)
			GetManageGamesInElympicsWindowData();

		manageGamesInElympicsWindowData.objectToSerialize = elympicsConfigSerializedObject.targetObject;
	}

	private void ConvertColorsFromHexToUnity()
	{
		if (ColorUtility.TryParseHtmlString(elympicsColorHex, out Color convertedColor))
		{
			elympicsColor = convertedColor;
		}

		colorsConverted = true;
	}

	private void OnGUI()
	{
		if (BuildPipeline.isBuildingPlayer || GetManageGamesInElympicsWindowData() == false)
			return;

		elympicsApiEndpointChecker.Update();
		elympicsLobbyEndpointChecker.Update();
		elympicsGameServersEndpointChecker.Update();

		PrepareDrawer();
		PrepareGUIStyleCalculator();

		if (!EditorPrefs.GetBool(ElympicsConfig.IsLoginKey))
		{
			DrawLoginToElympicsContent();
		}
		else
		{
			DrawManageGamesContent();
		}

		elympicsConfigSerializedObject.ApplyModifiedProperties();
	}

	private void PrepareDrawer()
	{
		if (!colorsConverted)
			ConvertColorsFromHexToUnity();

		if (customInspectorDrawer == null)
		{
			customInspectorDrawer = new CustomInspectorDrawer(position, 5, 15);
		}
		else
		{
			customInspectorDrawer.PrepareToDraw(position);
		}

		resizibleCenteredLabelWidth = (int) (position.width * 0.80f);
	}

	private void PrepareGUIStyleCalculator()
	{
		if (guiStyleWrappedTextCalculator == null)
		{
			guiStyleWrappedTextCalculator = new GUIStyle();
			guiStyleWrappedTextCalculator.richText = true;
			guiStyleWrappedTextCalculator.wordWrap = true;
		}
	}

	private void DrawManageGamesContent()
	{
		customInspectorDrawer.Space();

		DrawAccountSection();
		DrawEndpointsSection();
		DrawAvailableGamesSection();
	}

	private void DrawLoginToElympicsContent()
	{
		customInspectorDrawer.Space();

		DrawLoginSection();
		DrawLoginEndpointSection();
	}

	#region Elympics Endpoints Section

	private void DrawEndpointsSection()
	{
		customInspectorDrawer.DrawHeader("Endpoints", 20, elympicsColor);
		customInspectorDrawer.Space();

		customInspectorDrawer.DrawEndpoint("API Endpoint", elympicsApiEndpoint, elympicsApiEndpointChecker, 0.3f, 0.3f, out bool apiEndpointChanged);
		if (apiEndpointChanged)
			elympicsApiEndpointChecker.UpdateUri(elympicsApiEndpoint.stringValue);

		if (customInspectorDrawer.DrawButtonCentered("Synchronize", resizibleCenteredLabelWidth, 20))
		{
			if (!elympicsApiEndpointChecker.IsRequestSuccessful)
			{
				Debug.LogError("Cannot connect to API, check API Endpoint");
				return;
			}

			ElympicsWebIntegration.GetElympicsEndpoints(endpoint =>
			{
				elympicsLobbyEndpoint.SetValue(endpoint.Lobby);
				elympicsGameServersEndpoint.SetValue(endpoint.GameServers);
			});
			ElympicsWebIntegration.GetAvailableGames(availableGamesOnline =>
			{
				Debug.Log($"Received {availableGamesOnline.Count} games - {string.Join(", ", availableGamesOnline.Select(x => x.Name))}");
				accountGames = availableGamesOnline;
			});
			GUI.FocusControl(null);
		}

		customInspectorDrawer.DrawLabelCentered(synchronizeInfo, resizibleCenteredLabelWidth, 20, true);
		customInspectorDrawer.Space();

		customInspectorDrawer.DrawEndpoint("Lobby Endpoint", elympicsLobbyEndpoint, elympicsLobbyEndpointChecker, 0.3f, 0.3f, out bool lobbyEndpointChanged);
		if (lobbyEndpointChanged)
			elympicsLobbyEndpointChecker.UpdateUri(elympicsLobbyEndpoint.stringValue);

		customInspectorDrawer.DrawEndpoint("Game Servers Endpoint", elympicsGameServersEndpoint, elympicsGameServersEndpointChecker, 0.3f, 0.3f, out bool gameServersEndpointChanged);
		if (gameServersEndpointChanged)
			elympicsGameServersEndpointChecker.UpdateUri(elympicsGameServersEndpoint.stringValue);

		customInspectorDrawer.DrawAccountGames(accountGames);
	}

	#endregion

	#region Available Games Section

	private void DrawAvailableGamesSection()
	{
		customInspectorDrawer.DrawHeader("Local games configurations", 20, elympicsColor);

		var chosenGameProperty = GetChosenGameProperty();

		if (availableGames.GetValue() == null)
			availableGames.SetValue(new List<ElympicsGameConfig>());

		if (chosenGameProperty != null && chosenGameProperty.objectReferenceValue != null)
		{
			currentGameIndex.intValue = customInspectorDrawer.DrawPopup("Active game:", currentGameIndex.intValue, ((List<ElympicsGameConfig>) availableGames.GetValue()).Select(x => $"{x?.GameName} ({x?.GameId})").ToArray());
			customInspectorDrawer.DrawSerializedProperty("Local games configurations", availableGames);
			customInspectorDrawer.Space();

			ElympicsGameConfig activeGameConfig = ((List<ElympicsGameConfig>) availableGames.GetValue())[currentGameIndex.intValue];

			PrepareElympicsGameConfigDrawer(activeGameConfig);
			elympicsGameConfigInfoDrawer.DrawGeneralGameConfigInfo();

			DrawGameManagmentInElympicsSection(activeGameConfig);
		}
		else
		{
			DrawButtonForCreatingFirstSetup(availableGames, currentGameIndex);
		}

		elympicsConfigSerializedObject.ApplyModifiedProperties();
	}

	private void DrawButtonForCreatingFirstSetup(SerializedProperty availableGames, SerializedProperty currentGameIndex)
	{
		customInspectorDrawer.DrawLabelCentered(noGameSetupsInfo, resizibleCenteredLabelWidth, 40, true);

		if (customInspectorDrawer.DrawButtonCentered("Create first game config!", resizibleCenteredLabelWidth, 20))
		{
			var config = ScriptableObject.CreateInstance<ElympicsGameConfig>();
			AssetDatabase.CreateAsset(config, "Assets/Resources/Elympics/ElympicsGameConfig.asset");
			AssetDatabase.SaveAssets();
			availableGames.InsertArrayElementAtIndex(availableGames.arraySize);
			var value = availableGames.GetArrayElementAtIndex(availableGames.arraySize - 1);
			value.objectReferenceValue = config;
			currentGameIndex.intValue = availableGames.arraySize - 1;
		}

		var configs = AssetDatabase.FindAssets($"t:{nameof(ElympicsGameConfig)}");
		customInspectorDrawer.DrawLabelCentered(importExistingsGamesInfo, resizibleCenteredLabelWidth, 20, true);
		if (customInspectorDrawer.DrawButtonCentered($"Find and import games ({configs.Length})", resizibleCenteredLabelWidth, 20))
		{
			if (configs.Length <= 0)
			{
				Debug.LogWarning($"No {nameof(ElympicsGameConfig)} found in assets");
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
	}

	private void PrepareElympicsGameConfigDrawer(ElympicsGameConfig activeGameConfig)
	{
		if (elympicsGameConfigInfoDrawer == null)
		{
			elympicsGameConfigInfoDrawer = new ElympicsGameConfigGeneralInfoDrawer(customInspectorDrawer, elympicsColor);
		}

		if (elympicsGameConfigInfoDrawer.GameConfig != activeGameConfig)
			elympicsGameConfigInfoDrawer.SetGameConfigProperty(activeGameConfig);
	}

	private SerializedProperty GetChosenGameProperty()
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

	private void DrawGameManagmentInElympicsSection(ElympicsGameConfig activeGameConfig)
	{
		customInspectorDrawer.DrawHeader("Manage " + activeGameConfig.gameName + " in Elympics", 20, elympicsColor);
		customInspectorDrawer.Space();

		if (customInspectorDrawer.DrawButtonCentered("Upload", resizibleCenteredLabelWidth, 20))
		{
			if (!ElympicsWebIntegration.IsConnectedToElympics())
				return;

			ElympicsWebIntegration.UploadGame();
			GUIUtility.ExitGUI();
		}

		var wrappedLabelHeight = (int) guiStyleWrappedTextCalculator.CalcHeight(new GUIContent(uploadGameInfo), position.width * 0.8f);
		customInspectorDrawer.DrawLabelCentered(uploadGameInfo, resizibleCenteredLabelWidth, wrappedLabelHeight, true);
	}

	#endregion

	#region Account Section

	private void DrawAccountSection()
	{
		customInspectorDrawer.DrawHeader("Account", 20, elympicsColor);
		DrawHeaderLoggedAs(EditorPrefs.GetString(ElympicsConfig.UsernameKey));
		customInspectorDrawer.Space();
		DrawLogoutButtonCentered();
		customInspectorDrawer.Space();
	}

	private void DrawHeaderLoggedAs(string username)
	{
		string content = loggedAsInfo + $"<b>{username}</b>";

		customInspectorDrawer.DrawLabelCentered(content, 400, 20, false);
	}

	private void DrawLogoutButtonCentered()
	{
		if (customInspectorDrawer.DrawButtonCentered("Logout", 150, 30))
		{
			ElympicsWebIntegration.Logout();
			GUI.FocusControl(null);
		}
	}

	#endregion

	#region Login Section

	private void DrawLoginEndpointSection()
	{
		customInspectorDrawer.DrawHeader("Elympics Endpoint", 20, elympicsColor);
		customInspectorDrawer.Space();

		customInspectorDrawer.DrawEndpoint("Elympics Web Endpoint", elympicsApiEndpoint, elympicsApiEndpointChecker, 0.3f, 0.3f, out bool endpointChanged);

		if (endpointChanged)
			elympicsApiEndpointChecker.UpdateUri(elympicsApiEndpoint.stringValue);
	}

	private void DrawLoginSection()
	{
		customInspectorDrawer.DrawHeader("Account", 20, elympicsColor);
		customInspectorDrawer.DrawLabelCentered(loginHeaderInfo, 400, 20, false);

		DrawLoginUsernameKey(ElympicsConfig.UsernameKey);
		DrawLoginPasswordKey(ElympicsConfig.PasswordKey);

		customInspectorDrawer.Space();

		DrawLoginButton();
	}

	private void DrawLoginUsernameKey(string key)
	{
		customInspectorDrawer.DrawLabelCentered(key, 200, 20, false);
		EditorPrefs.SetString(key,
			customInspectorDrawer.DrawTextFieldCentered(EditorPrefs.GetString(key), 200, 20));
	}

	private void DrawLoginPasswordKey(string key)
	{
		customInspectorDrawer.DrawLabelCentered(key, 200, 20, false);
		EditorPrefs.SetString(key,
			customInspectorDrawer.DrawPasswordFieldCentered(EditorPrefs.GetString(key), 200, 20));
	}

	private void DrawLoginButton()
	{
		if (customInspectorDrawer.DrawButtonCentered("Login", 150, 30))
		{
			if (!elympicsApiEndpointChecker.IsRequestSuccessful)
			{
				Debug.LogError("cannot connect with elympicsweb, check elympicsweb endpoint");
				return;
			}

			ElympicsWebIntegration.Login();
		}
	}

	#endregion
}
