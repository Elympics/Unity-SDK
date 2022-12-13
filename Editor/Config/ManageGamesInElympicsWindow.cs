using Elympics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ManageGamesInElympicsWindow : EditorWindow
{
	#region Labels

	private const string windowTitle = "Manage games in Elympics";
	private const string loginHeaderInfo = "<i>You have to be logged in to manage games in Elympics!</i>";
	private const string loggedAsInfo = "Logged in <color=#2EACFF>ElympicsWeb</color> as ";

	private const string synchronizeInfo = "Synchronize endpoints and games available for your account";

	private const string noGameSetupsInfo = "<i>You don't have any available games yet. Click button below to create first Elympics Config!</i>";

	private const string uploadGameInfo = "Upload new version of game with current settings to Elympics, game name and game id in config should match with game in Elympics. " +
										  "It's required to first upload a game version if you want to play it in online mode.";

	private const string importExistingsGamesInfo = "<i>or import existing games (e.g. samples)</i>";

	#endregion

	#region Colors

	private bool colorsConverted = false;
	private const string elympicsColorHex = "#2EACFF";
	private Color elympicsColor = Color.blue;

	#endregion

	#region Data Received From Elympics Config

	private static SerializedObject elympicsConfigSerializedObject;
	private static SerializedProperty currentGameIndex;
	private static SerializedProperty availableGames;
	private static SerializedProperty elympicsApiEndpoint;
	private static EditorEndpointChecker elympicsApiEndpointChecker;
	private static SerializedProperty elympicsLobbyEndpoint;
	private static EditorEndpointChecker elympicsLobbyEndpointChecker;
	private static SerializedProperty elympicsGameServersEndpoint;
	private static EditorEndpointChecker elympicsGameServersEndpointChecker;

	#endregion

	private List<ElympicsWebIntegration.GameResponseModel> _accountGames;
	private CustomInspectorDrawer _customInspectorDrawer;
	private ElympicsGameConfigGeneralInfoDrawer _elympicsGameConfigInfoDrawer;
	private GUIStyle _guiStyleWrappedTextCalculator;

	private int _resizibleCenteredLabelWidth;

	private static ManageGamesInElympicsWindowData manageGamesInElympicsWindowData;

	public static ManageGamesInElympicsWindow ShowWindow(SerializedObject elympicsConfigSerializedObject, SerializedProperty currentGameIndex, SerializedProperty availableGames, SerializedProperty elympicsApiEndpoint,
		SerializedProperty elympicsLobbyEndpoint, SerializedProperty elympicsGameServersEndpoint)
	{
		ManageGamesInElympicsWindow.elympicsConfigSerializedObject = elympicsConfigSerializedObject;
		ManageGamesInElympicsWindow.currentGameIndex = currentGameIndex;
		ManageGamesInElympicsWindow.availableGames = availableGames;
		ManageGamesInElympicsWindow.elympicsApiEndpoint = elympicsApiEndpoint;
		ManageGamesInElympicsWindow.elympicsLobbyEndpoint = elympicsLobbyEndpoint;
		ManageGamesInElympicsWindow.elympicsGameServersEndpoint = elympicsGameServersEndpoint;

		elympicsApiEndpointChecker = new EditorEndpointChecker();
		elympicsApiEndpointChecker.UpdateUri(elympicsApiEndpoint.stringValue);
		elympicsLobbyEndpointChecker = new EditorEndpointChecker();
		elympicsLobbyEndpointChecker.UpdateUri(elympicsLobbyEndpoint.stringValue);
		elympicsGameServersEndpointChecker = new EditorEndpointChecker();
		elympicsGameServersEndpointChecker.UpdateUri(elympicsGameServersEndpoint.stringValue);

		manageGamesInElympicsWindowData = Resources.Load<ManageGamesInElympicsWindowData>("ManageGamesInElympicsWindowData");

		SaveDataToScriptableObject();

		var window = GetWindowWithRect<ManageGamesInElympicsWindow>(new Rect(0, 0, 500, 900), false, windowTitle);
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

		elympicsConfigSerializedObject = new SerializedObject(manageGamesInElympicsWindowData.objectToSerialize);
		currentGameIndex = elympicsConfigSerializedObject.FindProperty("currentGame");
		availableGames = elympicsConfigSerializedObject.FindProperty("availableGames");
		elympicsApiEndpoint = elympicsConfigSerializedObject.FindProperty("elympicsApiEndpoint");
		elympicsLobbyEndpoint = elympicsConfigSerializedObject.FindProperty("elympicsLobbyEndpoint");
		elympicsGameServersEndpoint = elympicsConfigSerializedObject.FindProperty("elympicsGameServersEndpoint");

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
		if (ColorUtility.TryParseHtmlString(elympicsColorHex, out var convertedColor))
			elympicsColor = convertedColor;

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

		if (!ElympicsConfig.IsLogin)
			DrawLoginToElympicsContent();
		else
			DrawManageGamesContent();

		elympicsConfigSerializedObject.ApplyModifiedProperties();
	}

	private void PrepareDrawer()
	{
		if (!colorsConverted)
			ConvertColorsFromHexToUnity();

		if (_customInspectorDrawer == null)
		{
			_customInspectorDrawer = new CustomInspectorDrawer(position, 5, 15);
		}
		else
		{
			_customInspectorDrawer.PrepareToDraw(position);
		}

		_resizibleCenteredLabelWidth = (int)(position.width * 0.80f);
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

		_customInspectorDrawer.DrawEndpoint("API Endpoint", elympicsApiEndpoint, elympicsApiEndpointChecker, 0.3f, 0.3f, out bool apiEndpointChanged);
		if (apiEndpointChanged)
			elympicsApiEndpointChecker.UpdateUri(elympicsApiEndpoint.stringValue);

		if (_customInspectorDrawer.DrawButtonCentered("Synchronize", _resizibleCenteredLabelWidth, 20))
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

				ElympicsWebIntegration.GetAvailableGames(availableGamesOnline =>
				{
					Debug.Log($"Received {availableGamesOnline.Count} games - {string.Join(", ", availableGamesOnline.Select(x => x.Name))}");
					_accountGames = availableGamesOnline;
				});
			});
			GUI.FocusControl(null);
		}

		_customInspectorDrawer.DrawLabelCentered(synchronizeInfo, _resizibleCenteredLabelWidth, 20, true);
		_customInspectorDrawer.Space();

		_customInspectorDrawer.DrawEndpoint("Lobby Endpoint", elympicsLobbyEndpoint, elympicsLobbyEndpointChecker, 0.3f, 0.3f, out bool lobbyEndpointChanged);
		if (lobbyEndpointChanged)
			elympicsLobbyEndpointChecker.UpdateUri(elympicsLobbyEndpoint.stringValue);

		_customInspectorDrawer.DrawEndpoint("Game Servers Endpoint", elympicsGameServersEndpoint, elympicsGameServersEndpointChecker, 0.3f, 0.3f, out bool gameServersEndpointChanged);
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

		if (chosenGameProperty != null && chosenGameProperty.objectReferenceValue != null)
		{
			currentGameIndex.intValue = _customInspectorDrawer.DrawPopup("Active game:", currentGameIndex.intValue,
				((List<ElympicsGameConfig>)availableGames.GetValue()).Select(x => $"{x?.GameName} ({x?.GameId})").ToArray());
			_customInspectorDrawer.DrawSerializedProperty("Local games configurations", availableGames);
			_customInspectorDrawer.Space();

			PrepareElympicsGameConfigDrawer(chosenGameProperty);
			_elympicsGameConfigInfoDrawer.DrawGeneralGameConfigInfo();

			DrawGameManagmentInElympicsSection(chosenGameProperty.objectReferenceValue as ElympicsGameConfig);

			_elympicsGameConfigInfoDrawer.ApplyModifications();
		}
		else
			DrawButtonForCreatingFirstSetup(availableGames, currentGameIndex);

		elympicsConfigSerializedObject.ApplyModifiedProperties();
	}

	private void DrawButtonForCreatingFirstSetup(SerializedProperty availableGames, SerializedProperty currentGameIndex)
	{
		_customInspectorDrawer.DrawLabelCentered(noGameSetupsInfo, _resizibleCenteredLabelWidth, 40, true);

		if (_customInspectorDrawer.DrawButtonCentered("Create first game config!", _resizibleCenteredLabelWidth, 20))
		{
			var config = ScriptableObject.CreateInstance<ElympicsGameConfig>();
			if (!Directory.Exists(ElympicsConfig.ELYMPICS_RESOURCES_PATH))
			{
				Debug.Log("Creating Elympics resources directory...");
				Directory.CreateDirectory(ElympicsConfig.ELYMPICS_RESOURCES_PATH);
			}
			AssetDatabase.CreateAsset(config, ElympicsConfig.ELYMPICS_RESOURCES_PATH + "/ElympicsGameConfig.asset");
			AssetDatabase.SaveAssets();
			availableGames.InsertArrayElementAtIndex(availableGames.arraySize);
			var value = availableGames.GetArrayElementAtIndex(availableGames.arraySize - 1);
			value.objectReferenceValue = config;
			currentGameIndex.intValue = availableGames.arraySize - 1;
		}

		var configs = AssetDatabase.FindAssets($"t:{nameof(ElympicsGameConfig)}");
		_customInspectorDrawer.DrawLabelCentered(importExistingsGamesInfo, _resizibleCenteredLabelWidth, 20, true);

		if (!_customInspectorDrawer.DrawButtonCentered($"Find and import games ({configs.Length})", _resizibleCenteredLabelWidth, 20))
			return;

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

	private void PrepareElympicsGameConfigDrawer(SerializedProperty activeGameConfig)
	{
		if (_elympicsGameConfigInfoDrawer == null)
			_elympicsGameConfigInfoDrawer = new ElympicsGameConfigGeneralInfoDrawer(_customInspectorDrawer, elympicsColor);

		_elympicsGameConfigInfoDrawer.UpdateGameConfigProperty(activeGameConfig);
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

	private void DrawGameManagmentInElympicsSection(ElympicsGameConfig activeGameConfig)
	{
		_customInspectorDrawer.DrawHeader("Manage " + activeGameConfig.GameName + " in Elympics", 20, elympicsColor);
		_customInspectorDrawer.Space();

		if (_customInspectorDrawer.DrawButtonCentered("Upload", _resizibleCenteredLabelWidth, 20))
		{
			if (!ElympicsWebIntegration.IsConnectedToElympics())
				return;

			ElympicsWebIntegration.BuildAndUploadGame();
			GUIUtility.ExitGUI();
		}

		var wrappedLabelHeight = (int)_guiStyleWrappedTextCalculator.CalcHeight(new GUIContent(uploadGameInfo), position.width * 0.8f);
		_customInspectorDrawer.DrawLabelCentered(uploadGameInfo, _resizibleCenteredLabelWidth, wrappedLabelHeight, true);
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
		string content = loggedAsInfo + $"<b>{username}</b>";

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
		_customInspectorDrawer.DrawHeader("Elympics Endpoint", 20, elympicsColor);
		_customInspectorDrawer.Space();

		_customInspectorDrawer.DrawEndpoint("Elympics Web Endpoint", elympicsApiEndpoint, elympicsApiEndpointChecker, 0.3f, 0.3f, out bool endpointChanged);

		if (endpointChanged)
			elympicsApiEndpointChecker.UpdateUri(elympicsApiEndpoint.stringValue);
	}

	private void DrawLoginSection()
	{
		_customInspectorDrawer.DrawHeader("Account", 20, elympicsColor);
		_customInspectorDrawer.DrawLabelCentered(loginHeaderInfo, 400, 20, false);

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
