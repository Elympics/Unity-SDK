using Elympics;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ManageGamesInElympicsWindow : EditorWindow
{
	#region Labels

	private const string windowTitle           = "Manage games in Elympics";
	private const string loginHeaderInfo       = "<i>You have to be logged in to manage games in Elympics!</i>";
	private const string loggedAsInfo          = "Logged in <color=#2EACFF>ElympicsWeb</color> as ";
	private const string createGameSummaryInfo = "Create new game in Elympics with current name and overwrite current config with new game id. It's required to first created game before upload a new version";

	private const string uploadSummaryInfo = "Upload new version of game with current settings to Elympics, game name and game id in config should match with game in Elympics. " +
	                                         "It's required to first upload a game version if you want to play it in online mode.";

	private const string noGameSetupsInfo         = "<i>You don't have any available games yet. Click button below to create first Elympics Config!</i>";
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
	private static SerializedProperty    elympicsWebEndPoint;
	private static EditorEndpointChecker elympicsWebEndpointChecker;
	private static SerializedProperty    elympicsEndPoint;
	private static EditorEndpointChecker elympicsEndpointChecker;

	#endregion

	private CustomInspectorDrawer               customInspectorDrawer         = null;
	private ElympicsGameConfigGeneralInfoDrawer elympicsGameConfigInfoDrawer  = null;
	private GUIStyle                            guiStyleWrappedTextCalculator = null;

	private int resizibleCenteredLabelWidth = 0;

	private static ManageGamesInElympicsWindowData manageGamesInElympicsWindowData = null;

	public static ManageGamesInElympicsWindow ShowWindow(SerializedObject elympicsConfigSerializedObject, SerializedProperty currentGameIndex, SerializedProperty availableGames, SerializedProperty elympicsWebEndPoint,
		SerializedProperty elympicsEndPoint)
	{
		ManageGamesInElympicsWindow.elympicsConfigSerializedObject = elympicsConfigSerializedObject;
		ManageGamesInElympicsWindow.currentGameIndex = currentGameIndex;
		ManageGamesInElympicsWindow.availableGames = availableGames;
		ManageGamesInElympicsWindow.elympicsWebEndPoint = elympicsWebEndPoint;
		ManageGamesInElympicsWindow.elympicsEndPoint = elympicsEndPoint;

		ManageGamesInElympicsWindow.elympicsWebEndpointChecker = new EditorEndpointChecker();
		ManageGamesInElympicsWindow.elympicsWebEndpointChecker.UpdateUri(elympicsWebEndPoint.stringValue);
		ManageGamesInElympicsWindow.elympicsEndpointChecker = new EditorEndpointChecker();
		ManageGamesInElympicsWindow.elympicsEndpointChecker.UpdateUri(elympicsEndPoint.stringValue);

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
		ManageGamesInElympicsWindow.elympicsWebEndPoint = elympicsConfigSerializedObject.FindProperty("elympicsWebEndpoint");
		ManageGamesInElympicsWindow.elympicsEndPoint = elympicsConfigSerializedObject.FindProperty("elympicsEndpoint");

		if (elympicsWebEndpointChecker == null || elympicsEndpointChecker == null)
		{
			elympicsWebEndpointChecker = new EditorEndpointChecker();
			elympicsEndpointChecker = new EditorEndpointChecker();
			elympicsWebEndpointChecker.UpdateUri(elympicsWebEndPoint.stringValue);
			elympicsEndpointChecker.UpdateUri(elympicsEndPoint.stringValue);
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

		elympicsWebEndpointChecker.Update();
		elympicsEndpointChecker.Update();

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

		customInspectorDrawer.DrawEndpoint("Elympics Web Endpoint", elympicsWebEndPoint, elympicsWebEndpointChecker, 0.3f, 0.3f, out bool webEndpointChanged);
		if (webEndpointChanged)
			elympicsWebEndpointChecker.UpdateUri(elympicsWebEndPoint.stringValue);

		customInspectorDrawer.DrawEndpoint("Elympics Endpoint", elympicsEndPoint, elympicsEndpointChecker, 0.3f, 0.3f, out bool endpointChanged);
		elympicsEndpointChecker.UpdateUri(elympicsEndPoint.stringValue);

		if (customInspectorDrawer.DrawButtonCentered("Synchronize", (int) (position.width * 0.80f), 20))
		{
			if (!elympicsWebEndpointChecker.IsRequestSuccessful)
			{
				Debug.LogError("Cannot connect with ElympicsWeb, check ElympicsWeb endpoint");
				return;
			}

			ElympicsWebIntegration.GetElympicsEndpoint(endpoint => elympicsEndPoint.SetValue(endpoint));
			GUI.FocusControl(null);
		}
	}

	#endregion

	#region Available Games Section

	private void DrawAvailableGamesSection()
	{
		customInspectorDrawer.DrawHeader("Available games", 20, elympicsColor);

		var chosenGameProperty = GetChosenGameProperty();

		if (availableGames.GetValue() == null)
			availableGames.SetValue(new List<ElympicsGameConfig>());

		if (chosenGameProperty != null && chosenGameProperty.objectReferenceValue != null)
		{
			currentGameIndex.intValue = customInspectorDrawer.DrawPopup("Active game:", currentGameIndex.intValue, ((List<ElympicsGameConfig>) availableGames.GetValue()).Select(x => $"{x?.GameName} ({x?.GameId})").ToArray());
			customInspectorDrawer.DrawSerializedProperty("Available games", availableGames);
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

		if (customInspectorDrawer.DrawButtonCentered("Create", resizibleCenteredLabelWidth, 20))
		{
			SerializedObject serializedGameConfig = new UnityEditor.SerializedObject(activeGameConfig);

			SerializedProperty gameName = serializedGameConfig.FindProperty("gameName");
			SerializedProperty gameId = serializedGameConfig.FindProperty("gameId");

			if (!ElympicsWebIntegration.IsConnectedToElympics())
				return;

			ElympicsWebIntegration.CreateGame(gameName, gameId);
		}

		int wrappedLabelHeight = (int) guiStyleWrappedTextCalculator.CalcHeight(new GUIContent(createGameSummaryInfo), position.width * 0.8f);
		customInspectorDrawer.DrawLabelCentered(createGameSummaryInfo, resizibleCenteredLabelWidth, wrappedLabelHeight, true);

		customInspectorDrawer.Space();

		if (customInspectorDrawer.DrawButtonCentered("Upload", resizibleCenteredLabelWidth, 20))
		{
			if (!ElympicsWebIntegration.IsConnectedToElympics())
				return;

			ElympicsWebIntegration.UploadGame();
			GUIUtility.ExitGUI();
		}

		wrappedLabelHeight = (int) guiStyleWrappedTextCalculator.CalcHeight(new GUIContent(uploadSummaryInfo), position.width * 0.8f);
		customInspectorDrawer.DrawLabelCentered(uploadSummaryInfo, resizibleCenteredLabelWidth, wrappedLabelHeight, true);
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

		customInspectorDrawer.DrawEndpoint("Elympics Web Endpoint", elympicsWebEndPoint, elympicsWebEndpointChecker, 0.3f, 0.3f, out bool endpointChanged);

		if (endpointChanged)
			elympicsWebEndpointChecker.UpdateUri(elympicsWebEndPoint.stringValue);
	}

	private void DrawLoginSection()
	{
		customInspectorDrawer.DrawHeader("Account", 20, elympicsColor);
		customInspectorDrawer.DrawLabelCentered(loginHeaderInfo, 400, 20, false);

		DrawLoginKey(ElympicsConfig.UsernameKey);
		DrawLoginKey(ElympicsConfig.PasswordKey);

		customInspectorDrawer.Space();

		DrawLoginButton();
	}

	private void DrawLoginKey(string key)
	{
		customInspectorDrawer.DrawLabelCentered(key, 200, 20, false);
		EditorPrefs.SetString(key,
			customInspectorDrawer.DrawTextFieldCentered(EditorPrefs.GetString(key), 200, 20));
	}

	private void DrawLoginButton()
	{
		if (customInspectorDrawer.DrawButtonCentered("Login", 150, 30))
		{
			if (!elympicsWebEndpointChecker.IsRequestSuccessful)
			{
				Debug.LogError("cannot connect with elympicsweb, check elympicsweb endpoint");
				return;
			}

			ElympicsWebIntegration.Login();
		}
	}

	#endregion
}
