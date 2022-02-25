using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Elympics
{
	internal class CustomInspectorDrawer
	{
		private const int defaultOneLineHeight = 20;

		private int spacingBetweenElements      = 0;
		private int currentSpaceBetweenElements = 0;
		private int horizontalMargin            = 0;
		private int HorizontalMarginsWidth => horizontalMargin * 2;

		private Rect     position;
		private GUIStyle centeredStyleLabel        = null;
		private GUIStyle centeredStyleLabelWrapped = null;
		private GUIStyle centeredStyleTextField    = null;
		private GUIStyle headerStyleLabel          = null;

		public CustomInspectorDrawer(Rect position, int spacingBetweenElements, int horizontalMargin)
		{
			this.position = position;
			this.horizontalMargin = horizontalMargin;
			this.spacingBetweenElements = spacingBetweenElements;
		}

		public void PrepareToDraw(Rect position)
		{
			this.position = position;

			currentSpaceBetweenElements = 0;

			PrepareStyles();
		}

		private void PrepareStyles()
		{
			if (centeredStyleLabel == null || centeredStyleTextField == null || centeredStyleLabelWrapped == null)
			{
				centeredStyleLabel = new GUIStyle(GUI.skin.label)
				{
					alignment = TextAnchor.MiddleCenter,
					richText = true
				};

				centeredStyleTextField = new GUIStyle(GUI.skin.textField)
				{
					alignment = TextAnchor.MiddleCenter,
					richText = true
				};

				centeredStyleLabelWrapped = new GUIStyle(GUI.skin.label)
				{
					alignment = TextAnchor.MiddleCenter,
					richText = true,
					wordWrap = true
				};
			}

			if (headerStyleLabel == null)
			{
				headerStyleLabel = new GUIStyle(GUI.skin.label)
				{
					alignment = TextAnchor.MiddleLeft,
					richText = true
				};
			}
		}

		public void DrawLabelCentered(string content, int width, int height, bool textWrapped)
		{
			EditorGUI.LabelField(new Rect((position.width / 2) - (width / 2), currentSpaceBetweenElements, width, height), content, textWrapped ? centeredStyleLabelWrapped : centeredStyleLabel);
			IncreaseSpacingManually(height + spacingBetweenElements);
		}

		public string DrawTextFieldCentered(string content, int width, int height)
		{
			var result = EditorGUI.TextField(new Rect((position.width / 2) - (width / 2), currentSpaceBetweenElements, width, height), content, centeredStyleTextField);
			IncreaseSpacingManually(height + spacingBetweenElements);

			return result;
		}

		public bool DrawButtonCentered(string content, int width, int height)
		{
			var result = GUI.Button(new Rect((position.width / 2) - (width / 2), currentSpaceBetweenElements, width, height), content);
			IncreaseSpacingManually(height + spacingBetweenElements);

			return result;
		}

		public void Space()
		{
			IncreaseSpacingManually(spacingBetweenElements);
		}

		public void IncreaseSpacingManually(int value)
		{
			currentSpaceBetweenElements += value;
		}

		public void DrawHeader(string content, int height, Color headerLineColor)
		{
			string headerBold = $"<b>{content}</b>";

			EditorGUI.LabelField(new Rect(horizontalMargin, currentSpaceBetweenElements, position.width - HorizontalMarginsWidth, height), headerBold, headerStyleLabel);
			IncreaseSpacingManually(height);

			EditorGUI.DrawRect(new Rect(horizontalMargin, currentSpaceBetweenElements, position.width - HorizontalMarginsWidth, 1), headerLineColor);
			Space();
		}

		public void DrawSerializedProperty(string content, SerializedProperty property)
		{
			int height = (int) EditorGUI.GetPropertyHeight(property, true);

			EditorGUI.PropertyField(new Rect(horizontalMargin, currentSpaceBetweenElements, position.width - HorizontalMarginsWidth, height), property, new GUIContent(content), true);
			IncreaseSpacingManually(height + spacingBetweenElements);
		}

		public int DrawPopup(string content, int currentIndex, string[] displayedOptions)
		{
			int result = EditorGUI.Popup(new Rect(horizontalMargin, currentSpaceBetweenElements, position.width - HorizontalMarginsWidth, defaultOneLineHeight), content, currentIndex, displayedOptions);
			IncreaseSpacingManually(defaultOneLineHeight + spacingBetweenElements);
			return result;
		}

		public string DrawStringField(string header, string content, float headerWidthPercentage01, bool canBeModified)
		{
			headerWidthPercentage01 = Mathf.Clamp(headerWidthPercentage01, 0.0f, 1.0f);
			float headerWidth = position.width * headerWidthPercentage01;

			EditorGUI.LabelField(new Rect(horizontalMargin, currentSpaceBetweenElements, headerWidth - horizontalMargin, defaultOneLineHeight), header, centeredStyleLabel);

			EditorGUI.BeginDisabledGroup(!canBeModified);
			var result = EditorGUI.TextField(new Rect(headerWidth + horizontalMargin, currentSpaceBetweenElements, (position.width - headerWidth) - HorizontalMarginsWidth, defaultOneLineHeight), content, centeredStyleTextField);
			EditorGUI.EndDisabledGroup();

			IncreaseSpacingManually(defaultOneLineHeight + spacingBetweenElements);

			return result;
		}

		public int DrawIntField(string header, int content, float headerWidthPercentage01, bool canBeModified)
		{
			headerWidthPercentage01 = Mathf.Clamp(headerWidthPercentage01, 0.0f, 1.0f);
			float headerWidth = position.width * headerWidthPercentage01;

			EditorGUI.LabelField(new Rect(horizontalMargin, currentSpaceBetweenElements, headerWidth - horizontalMargin, defaultOneLineHeight), header, centeredStyleLabel);

			EditorGUI.BeginDisabledGroup(!canBeModified);
			var result = EditorGUI.IntField(new Rect(headerWidth + horizontalMargin, currentSpaceBetweenElements, (position.width - headerWidth) - HorizontalMarginsWidth, defaultOneLineHeight), content, centeredStyleTextField);
			EditorGUI.EndDisabledGroup();

			IncreaseSpacingManually(defaultOneLineHeight + spacingBetweenElements);

			return result;
		}

		public UnityEngine.Object DrawSceneFieldWithOpenSceneButton(string header, UnityEngine.Object scene, float headerWidthPercentage01, float openeSceneButtonWidthPercentage01, out bool sceneFieldChanged,
			out bool openSceneButtonPressed)
		{
			headerWidthPercentage01 = Mathf.Clamp(headerWidthPercentage01, 0.0f, 1.0f);
			float headerWidth = position.width * headerWidthPercentage01;

			openeSceneButtonWidthPercentage01 = Mathf.Clamp(openeSceneButtonWidthPercentage01, 0.0f, 1.0f);
			float openSceneButtonWidth = (position.width - headerWidth) * openeSceneButtonWidthPercentage01;

			EditorGUI.LabelField(new Rect(horizontalMargin, currentSpaceBetweenElements, headerWidth - horizontalMargin, defaultOneLineHeight), header, centeredStyleLabel);

			EditorGUI.BeginChangeCheck();
			var result = EditorGUI.ObjectField(new Rect(headerWidth + horizontalMargin, currentSpaceBetweenElements, (position.width - headerWidth) - openSceneButtonWidth, defaultOneLineHeight), scene, typeof(SceneAsset), true);
			sceneFieldChanged = EditorGUI.EndChangeCheck();

			openSceneButtonPressed =
				GUI.Button(new Rect(headerWidth + horizontalMargin + ((position.width - headerWidth) - openSceneButtonWidth), currentSpaceBetweenElements, openSceneButtonWidth - HorizontalMarginsWidth, defaultOneLineHeight), "Open Scene");

			IncreaseSpacingManually(defaultOneLineHeight + spacingBetweenElements);

			return result;
		}

		public void DrawEndpoint(string content, SerializedProperty endpoint, EditorEndpointChecker endpointChecker, float headerWidthPercentage01, float callbackIndicatorWidthPercentage01, out bool endpointChanged)
		{
			int height = (int) EditorGUI.GetPropertyHeight(endpoint, true);

			headerWidthPercentage01 = Mathf.Clamp(headerWidthPercentage01, 0.0f, 1.0f);
			float headerWidth = position.width * headerWidthPercentage01;

			callbackIndicatorWidthPercentage01 = Mathf.Clamp(callbackIndicatorWidthPercentage01, 0.0f, 1.0f);
			float callbackIndicatorWidth = (position.width - headerWidth) * callbackIndicatorWidthPercentage01;

			EditorGUI.LabelField(new Rect(horizontalMargin, currentSpaceBetweenElements, headerWidth - horizontalMargin, height), content, centeredStyleLabel);

			EditorGUI.BeginChangeCheck();
			endpoint.stringValue = EditorGUI.TextField(new Rect(headerWidth + horizontalMargin, currentSpaceBetweenElements, (position.width - headerWidth) - callbackIndicatorWidth, height), endpoint.stringValue);
			endpointChanged = EditorGUI.EndChangeCheck();

			EditorGUI.LabelField(new Rect(headerWidth + horizontalMargin + ((position.width - headerWidth) - callbackIndicatorWidth), currentSpaceBetweenElements, callbackIndicatorWidth - HorizontalMarginsWidth, height),
				GetEndpointIndicator(endpointChecker), centeredStyleLabel);

			IncreaseSpacingManually(height + spacingBetweenElements);
		}

		private string GetEndpointIndicator(EditorEndpointChecker endpointChecker)
		{
			if (!endpointChecker.IsUriCorrect)
			{
				//yellow
				return "<color=#ECC70C>Wrong uri</color>";
			}
			else if (!endpointChecker.IsRequestDone)
			{
				//blue
				return "<color=#0061E1>Connecting...</color>";
			}
			else if (!endpointChecker.IsRequestSuccessful)
			{
				//red
				return "<color=#E1001E>Didn't connect</color>";
			}
			else
			{
				//green
				return "<color=#00E10F>Connected!</color>";
			}
		}

		private Vector2 scrollPos;

		public void DrawAccountGames(List<ElympicsWebIntegration.GameResponseModel> accountGames)
		{
			if (accountGames == null || accountGames.Count == 0)
				return;

			const int maxLinesInGroup = 4;
			var linesInGroup = Math.Min(accountGames.Count, maxLinesInGroup);

			EditorGUI.LabelField(new Rect(horizontalMargin, currentSpaceBetweenElements, position.width - horizontalMargin * 2, defaultOneLineHeight), "<b>Account Games</b> - use this data to fill local game configs", headerStyleLabel);
			IncreaseSpacingManually(defaultOneLineHeight + spacingBetweenElements);
			using (var area = new GUILayout.AreaScope(new Rect(horizontalMargin, currentSpaceBetweenElements, position.width, linesInGroup * defaultOneLineHeight)))
			{
				using (var scrollView = new GUILayout.ScrollViewScope(scrollPos, GUILayout.Width(position.width - horizontalMargin * 2), GUILayout.MaxHeight(linesInGroup * defaultOneLineHeight)))
				{
					scrollPos = scrollView.scrollPosition;

					var headerWidth = position.width * 0.25f;

					foreach (var responseModel in accountGames)
					{
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.TextField(responseModel.Name, GUILayout.Width(headerWidth));
						EditorGUILayout.TextField(responseModel.Id);
						EditorGUILayout.EndHorizontal();
					}
				}

				IncreaseSpacingManually(linesInGroup * defaultOneLineHeight + spacingBetweenElements);
			}
		}
	}
}
