using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Elympics
{
    internal class CustomInspectorDrawer
    {
        private const int DefaultOneLineHeight = 20;

        private int spacingBetweenElements = 0;
        private int currentSpaceBetweenElements = 0;
        private int horizontalMargin = 0;
        private int HorizontalMarginsWidth => horizontalMargin * 2;

        private Rect position;
        private GUIStyle centeredStyleLabel = null;
        private GUIStyle centeredStyleLabelWrapped = null;
        private GUIStyle centeredStyleTextField = null;
        private GUIStyle headerStyleLabel = null;

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

            headerStyleLabel ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                richText = true
            };
        }

        public void DrawLabelCentered(string content, int width, int height, bool textWrapped)
        {
            EditorGUI.LabelField(new Rect(position.width / 2 - width / 2, currentSpaceBetweenElements, width, height), content, textWrapped ? centeredStyleLabelWrapped : centeredStyleLabel);
            IncreaseSpacingManually(height + spacingBetweenElements);
        }

        public string DrawTextFieldCentered(string content, int width, int height)
        {
            var result = EditorGUI.TextField(new Rect(position.width / 2 - width / 2, currentSpaceBetweenElements, width, height), content, centeredStyleTextField);
            IncreaseSpacingManually(height + spacingBetweenElements);

            return result;
        }

        public string DrawPasswordFieldCentered(string content, int width, int height)
        {
            var result = EditorGUI.PasswordField(new Rect(position.width / 2 - width / 2, currentSpaceBetweenElements, width, height), content, centeredStyleTextField);
            IncreaseSpacingManually(height + spacingBetweenElements);

            return result;
        }

        public bool DrawButtonCentered(string content, int width, int height)
        {
            var result = GUI.Button(new Rect(position.width / 2 - width / 2, currentSpaceBetweenElements, width, height), content);
            IncreaseSpacingManually(height + spacingBetweenElements);

            return result;
        }

        public void DrawHelpBox(string content, int height, MessageType type)
        {
            EditorGUI.HelpBox(new Rect(horizontalMargin, currentSpaceBetweenElements, position.width, height), content, type);
            IncreaseSpacingManually(height + spacingBetweenElements);
        }

        public void Space() => IncreaseSpacingManually(spacingBetweenElements);

        public void IncreaseSpacingManually(int value) => currentSpaceBetweenElements += value;

        public void DrawHeader(string content, int height, Color headerLineColor)
        {
            var headerBold = $"<b>{content}</b>";

            EditorGUI.LabelField(new Rect(horizontalMargin, currentSpaceBetweenElements, position.width - HorizontalMarginsWidth, height), headerBold, headerStyleLabel);
            IncreaseSpacingManually(height);

            EditorGUI.DrawRect(new Rect(horizontalMargin, currentSpaceBetweenElements, position.width - HorizontalMarginsWidth, 1), headerLineColor);
            Space();
        }

        public void DrawSerializedProperty(string content, SerializedProperty property)
        {
            var height = (int)EditorGUI.GetPropertyHeight(property, true);

            _ = EditorGUI.PropertyField(new Rect(horizontalMargin, currentSpaceBetweenElements, position.width - HorizontalMarginsWidth, height), property, new GUIContent(content), true);
            IncreaseSpacingManually(height + spacingBetweenElements);
        }

        public int DrawPopup(string content, int currentIndex, string[] displayedOptions)
        {
            var result = EditorGUI.Popup(new Rect(horizontalMargin, currentSpaceBetweenElements, position.width - HorizontalMarginsWidth, DefaultOneLineHeight), content, currentIndex, displayedOptions);
            IncreaseSpacingManually(DefaultOneLineHeight + spacingBetweenElements);
            return result;
        }

        public string DrawStringField(string header, string content, float headerWidthPercentage01, bool canBeModified)
        {
            headerWidthPercentage01 = Mathf.Clamp(headerWidthPercentage01, 0.0f, 1.0f);
            var headerWidth = position.width * headerWidthPercentage01;

            EditorGUI.LabelField(new Rect(horizontalMargin, currentSpaceBetweenElements, headerWidth - horizontalMargin, DefaultOneLineHeight), header, centeredStyleLabel);

            EditorGUI.BeginDisabledGroup(!canBeModified);
            var result = EditorGUI.TextField(new Rect(headerWidth + horizontalMargin, currentSpaceBetweenElements, position.width - headerWidth - HorizontalMarginsWidth, DefaultOneLineHeight), content, centeredStyleTextField);
            EditorGUI.EndDisabledGroup();

            IncreaseSpacingManually(DefaultOneLineHeight + spacingBetweenElements);

            return result;
        }

        public int DrawIntField(string header, int content, float headerWidthPercentage01, bool canBeModified)
        {
            headerWidthPercentage01 = Mathf.Clamp(headerWidthPercentage01, 0.0f, 1.0f);
            var headerWidth = position.width * headerWidthPercentage01;

            EditorGUI.LabelField(new Rect(horizontalMargin, currentSpaceBetweenElements, headerWidth - horizontalMargin, DefaultOneLineHeight), header, centeredStyleLabel);

            EditorGUI.BeginDisabledGroup(!canBeModified);
            var result = EditorGUI.IntField(new Rect(headerWidth + horizontalMargin, currentSpaceBetweenElements, position.width - headerWidth - HorizontalMarginsWidth, DefaultOneLineHeight), content, centeredStyleTextField);
            EditorGUI.EndDisabledGroup();

            IncreaseSpacingManually(DefaultOneLineHeight + spacingBetweenElements);

            return result;
        }

        public UnityEngine.Object DrawSceneFieldWithOpenSceneButton(string header, UnityEngine.Object scene, float headerWidthPercentage01, float openSceneButtonWidthPercentage01, out bool sceneFieldChanged, out bool openSceneButtonPressed)
        {
            headerWidthPercentage01 = Mathf.Clamp(headerWidthPercentage01, 0.0f, 1.0f);
            var headerWidth = position.width * headerWidthPercentage01;

            openSceneButtonWidthPercentage01 = Mathf.Clamp(openSceneButtonWidthPercentage01, 0.0f, 1.0f);
            var openSceneButtonWidth = (position.width - headerWidth) * openSceneButtonWidthPercentage01;

            EditorGUI.LabelField(new Rect(horizontalMargin, currentSpaceBetweenElements, headerWidth - horizontalMargin, DefaultOneLineHeight), header, centeredStyleLabel);

            EditorGUI.BeginChangeCheck();
            var result = EditorGUI.ObjectField(new Rect(headerWidth + horizontalMargin, currentSpaceBetweenElements, position.width - headerWidth - openSceneButtonWidth, DefaultOneLineHeight), scene, typeof(SceneAsset), true);
            sceneFieldChanged = EditorGUI.EndChangeCheck();

            openSceneButtonPressed = GUI.Button(new Rect(headerWidth + horizontalMargin + (position.width - headerWidth - openSceneButtonWidth), currentSpaceBetweenElements, openSceneButtonWidth - HorizontalMarginsWidth, DefaultOneLineHeight), "Open Scene");

            IncreaseSpacingManually(DefaultOneLineHeight + spacingBetweenElements);

            return result;
        }

        public void DrawEndpoint(string content, SerializedProperty endpoint, EditorEndpointChecker endpointChecker, float headerWidthPercentage01, float callbackIndicatorWidthPercentage01, out bool endpointChanged)
        {
            var height = (int)EditorGUI.GetPropertyHeight(endpoint, true);

            headerWidthPercentage01 = Mathf.Clamp(headerWidthPercentage01, 0.0f, 1.0f);
            var headerWidth = position.width * headerWidthPercentage01;

            callbackIndicatorWidthPercentage01 = Mathf.Clamp(callbackIndicatorWidthPercentage01, 0.0f, 1.0f);
            var callbackIndicatorWidth = (position.width - headerWidth) * callbackIndicatorWidthPercentage01;

            EditorGUI.LabelField(new Rect(horizontalMargin, currentSpaceBetweenElements, headerWidth - horizontalMargin, height), content, centeredStyleLabel);

            EditorGUI.BeginChangeCheck();
            endpoint.stringValue = EditorGUI.TextField(new Rect(headerWidth + horizontalMargin, currentSpaceBetweenElements, position.width - headerWidth - callbackIndicatorWidth, height), endpoint.stringValue);
            endpointChanged = EditorGUI.EndChangeCheck();

            EditorGUI.LabelField(new Rect(headerWidth + horizontalMargin + (position.width - headerWidth - callbackIndicatorWidth), currentSpaceBetweenElements, callbackIndicatorWidth - HorizontalMarginsWidth, height), GetEndpointIndicator(endpointChecker), centeredStyleLabel);

            IncreaseSpacingManually(height + spacingBetweenElements);
        }

        private static string GetEndpointIndicator(EditorEndpointChecker endpointChecker)
        {
            if (!endpointChecker.IsUriCorrect)
                return "<color=#ECC70C>Wrong uri</color>";  // yellow
            if (!endpointChecker.IsRequestDone)
                return "<color=#0061E1>Connecting...</color>";  // blue
            return endpointChecker.IsRequestSuccessful
                ? "<color=#63DE4B>Connected!</color>"  // green
                : "<color=#E1001E>Didn't connect</color>";  // red
        }

        private Vector2 scrollPos;

        public void DrawAccountGames(List<ElympicsWebIntegration.GameResponseModel> accountGames)
        {
            if (accountGames == null || accountGames.Count == 0)
                return;

            const int maxLinesInGroup = 4;
            var linesInGroup = Math.Min(accountGames.Count, maxLinesInGroup);

            EditorGUI.LabelField(new Rect(horizontalMargin, currentSpaceBetweenElements, position.width - horizontalMargin * 2, DefaultOneLineHeight), "<b>Account Games</b> - use this data to fill local game configs", headerStyleLabel);
            IncreaseSpacingManually(DefaultOneLineHeight + spacingBetweenElements);
            using var area = new GUILayout.AreaScope(new Rect(horizontalMargin, currentSpaceBetweenElements, position.width, linesInGroup * DefaultOneLineHeight));
            using (var scrollView = new GUILayout.ScrollViewScope(scrollPos, GUILayout.Width(position.width - horizontalMargin * 2), GUILayout.MaxHeight(linesInGroup * DefaultOneLineHeight)))
            {
                scrollPos = scrollView.scrollPosition;

                var headerWidth = position.width * 0.25f;

                foreach (var responseModel in accountGames)
                {
                    _ = EditorGUILayout.BeginHorizontal();
                    _ = EditorGUILayout.TextField(responseModel.Name, GUILayout.Width(headerWidth));
                    _ = EditorGUILayout.TextField(responseModel.Id);
                    EditorGUILayout.EndHorizontal();
                }
            }

            IncreaseSpacingManually(linesInGroup * DefaultOneLineHeight + spacingBetweenElements);
        }

        public void DrawAvailableRegions(List<string> availableRegions)
        {
            if (availableRegions == null)
            {
                EditorGUI.LabelField(new Rect(horizontalMargin, currentSpaceBetweenElements, position.width - horizontalMargin * 2, DefaultOneLineHeight), "Click Synchronize button to retrieve available regions for active game.", headerStyleLabel);
            }
            else if (availableRegions.Count == 0)
            {
                EditorGUI.LabelField(new Rect(horizontalMargin, currentSpaceBetweenElements, position.width - horizontalMargin * 2, DefaultOneLineHeight), "No available regions for active game.", headerStyleLabel);
            }
            else
            {
                EditorGUI.LabelField(new Rect(horizontalMargin, currentSpaceBetweenElements, position.width - horizontalMargin * 2, DefaultOneLineHeight), "Available regions for active game:", headerStyleLabel);
                foreach (var region in availableRegions)
                {
                    if (string.IsNullOrEmpty(region))
                    {
                        continue;
                    }

                    _ = DrawStringField(string.Empty, region, 0f, true);
                }
            }

            IncreaseSpacingManually(DefaultOneLineHeight + spacingBetweenElements);
        }
    }
}
