using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Elympics
{
	[CustomEditor(typeof(ElympicsConfig))]
	public class ElympicsConfigEditor : Editor
	{
		private SerializedProperty _currentGameIndex;
		private SerializedProperty _availableGames;

		private Object _lastChosenGamePropertyObject;
		private Editor _lastChosenGameEditor;
		private SerializedProperty _elympicsApiEndpoint;
		private SerializedProperty _elympicsLobbyEndpoint;
		private SerializedProperty _elympicsGameServersEndpoint;
		private SerializedProperty _elympicVersion;
		private string _cachedSdkVersion = string.Empty;


		private void OnEnable()
		{
			CheckSdkVersion();
			_cachedSdkVersion = _elympicVersion.stringValue;
			_elympicsApiEndpoint = serializedObject.FindProperty("elympicsApiEndpoint");
			_elympicsLobbyEndpoint = serializedObject.FindProperty("elympicsLobbyEndpoint");
			_elympicsGameServersEndpoint = serializedObject.FindProperty("elympicsGameServersEndpoint");
			_currentGameIndex = serializedObject.FindProperty("currentGame");
			_availableGames = serializedObject.FindProperty("availableGames");
			var chosenGameProperty = GetChosenGameProperty();
			CreateChosenGameEditorIfChanged(chosenGameProperty);
		}

		private void OnValidate()
		{
			CheckSdkVersion();
			_cachedSdkVersion = _elympicVersion.stringValue;
		}

		private void CheckSdkVersion()
		{
			if (_elympicVersion == null)
			{
				_elympicVersion = serializedObject.FindProperty("elympicsVersion");
			}

			if (!EditorApplication.isPlayingOrWillChangePlaymode)
			{
				ElympicsVersionRetriever.GetVersion((version) => { _cachedSdkVersion = version; }
				);
			}
		}

		private SerializedProperty GetChosenGameProperty()
		{
			var chosen = _currentGameIndex.intValue;
			if (chosen < 0 || chosen >= _availableGames.arraySize)
				return null;

			return _availableGames.GetArrayElementAtIndex(chosen);
		}

		private void CreateChosenGameEditorIfChanged(SerializedProperty chosenGameProperty)
		{
			if (chosenGameProperty == null || chosenGameProperty.objectReferenceValue == _lastChosenGamePropertyObject)
				return;

			if (_lastChosenGameEditor != null)
				DestroyImmediate(_lastChosenGameEditor);
			_lastChosenGameEditor = CreateEditor(chosenGameProperty.objectReferenceValue);
			_lastChosenGamePropertyObject = chosenGameProperty.objectReferenceValue;
		}

		// TODO: make a user friendly version if there is no available game config (for example, during first time elympics setup)
		//       https://gitlab.app.daftmobile.com/elympics/unity-sdk/-/issues/112

		public override void OnInspectorGUI()
		{
			EditorGUILayout.Space(5);
			serializedObject.Update();
			EditorStyles.label.wordWrap = true;

			DrawSdkVersion();
			DrawEndpointsSection();
			DrawButtonManageGamesInElympics();

			var chosenGameProperty = GetChosenGameProperty();

			if (_availableGames.GetValue() == null)
				_availableGames.SetValue(new List<ElympicsGameConfig>());

			

			if (chosenGameProperty != null && chosenGameProperty.objectReferenceValue != null)
			{
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.PropertyField(_availableGames, new GUIContent("Local games configurations"), true);

				EditorGUILayout.Popup(new GUIContent("Active game"), _currentGameIndex.intValue,
					((List<ElympicsGameConfig>)_availableGames.GetValue()).Select(x => $"{x?.GameName} - {x?.GameId}")
					.ToArray());
				EditorGUI.EndDisabledGroup();

				DrawTitle(chosenGameProperty);
				DrawGameEditor(chosenGameProperty);
			}
			else
			{
				DrawNoAvailableGamesLabel();
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawSdkVersion()
		{
			var currentVersion = _elympicVersion.stringValue;
			if (string.IsNullOrEmpty(currentVersion) || currentVersion != _cachedSdkVersion)
			{
				_elympicVersion.SetValue(_cachedSdkVersion);
			}
			EditorGUILayout.LabelField($"Elympics SDK Version: {_elympicVersion.stringValue} ");
		}

		private void DrawNoAvailableGamesLabel()
		{
			GUILayout.Label(
				"You don't have any available game config yet. Create one in Manage Games in Elympics window!",
				EditorStyles.wordWrappedLabel);
		}

		public void DrawButtonManageGamesInElympics()
		{
			if (GUILayout.Button("Manage games in Elympics"))
			{
				ManageGamesInElympicsWindow.ShowWindow(serializedObject, _currentGameIndex, _availableGames,
					_elympicsApiEndpoint, _elympicsLobbyEndpoint, _elympicsGameServersEndpoint);
			}

			EditorGUILayout.Separator();
		}

		private void DrawEndpointsSection()
		{
			EditorEndpointCheckerEditor.DrawEndpointField(_elympicsApiEndpoint, "API endpoint");
			EditorEndpointCheckerEditor.DrawEndpointField(_elympicsLobbyEndpoint, "Lobby endpoint");
			EditorEndpointCheckerEditor.DrawEndpointField(_elympicsGameServersEndpoint, "GameServers endpoint");
			EditorGUILayout.Separator();
		}

		private static void DrawTitle(SerializedProperty chosenGameProperty)
		{
			var labelStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 20,
				alignment = TextAnchor.MiddleLeft
			};
			EditorGUILayout.Separator();
			EditorGUILayout.Space();
			var elympicsGameConfig = (ElympicsGameConfig)chosenGameProperty.GetValue();
			EditorGUILayout.LabelField($"{elympicsGameConfig.GameName}", labelStyle);
			EditorGUILayout.Space();
		}

		private void DrawGameEditor(SerializedProperty chosenGameProperty)
		{
			var gameStyle = new GUIStyle { margin = new RectOffset(10, 0, 0, 0) };
			EditorGUILayout.BeginVertical(gameStyle);
			CreateChosenGameEditorIfChanged(chosenGameProperty);
			_lastChosenGameEditor.OnInspectorGUI();
			EditorGUILayout.EndVertical();
		}
	}
}