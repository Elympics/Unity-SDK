#nullable enable

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Elympics.Editor.Config
{
    [CustomEditor(typeof(ElympicsConfig))]
    internal class ElympicsConfigEditor : UnityEditor.Editor
    {
        public VisualTreeAsset? inspectorUxml;

        private (Object Reference, VisualElement Inspector)? _currentGameConfig;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            if (inspectorUxml != null)
                root.Add(PrepareInspectorTree(inspectorUxml));
            return root;
        }

        // TODO: make a user friendly version if there is no available game config (for example, during first time elympics setup)
        //       https://gitlab.app.daftmobile.com/elympics/unity-sdk/-/issues/112

        private VisualElement PrepareInspectorTree(VisualTreeAsset sourceTree)
        {
            VisualElement inspectorTree = sourceTree.CloneTree();

            var config = (ElympicsConfig)serializedObject.targetObject;

            var sdkVersion = inspectorTree.Q<Label>("sdk-version");
            var availableGames = inspectorTree.Q<ListView>("available-games");
            var chosenGameConfig = inspectorTree.Q<GroupBox>("chosen-game-config");
            var gameTitle = inspectorTree.Q<Label>("game-title");
            var gameConfigNestingRoot = inspectorTree.Q<GroupBox>("game-config-nesting-root");
            var noGameConfigInfo = inspectorTree.Q<HelpBox>("no-game-config-info");
            var bootSceneAsset = inspectorTree.Q<ObjectField>("boot-scene-object");
            var bootScenePath = inspectorTree.Q<TextField>("boot-scene-path");
            var launcherBuildProblems = inspectorTree.Q<HelpBox>("launcher-build-problems");

            _ = bootSceneAsset.RegisterValueChangedCallback(evt =>
            {
                var asset = evt.newValue as SceneAsset;
                // Written to the bound field only on user change: a write before Bind() would be discarded.
                bootScenePath.value = asset != null ? AssetDatabase.GetAssetOrScenePath(asset) : string.Empty;
            });
            // Deferred: the change reaches the ElympicsConfig asset only after the binding applies it.
            _ = bootScenePath.RegisterValueChangedCallback(_ => inspectorTree.schedule.Execute(UpdateLauncherBuildProblems));
            EditorBuildSettings.sceneListChanged += UpdateLauncherBuildProblems;
            inspectorTree.RegisterCallback<DetachFromPanelEvent>(_ => EditorBuildSettings.sceneListChanged -= UpdateLauncherBuildProblems);

            availableGames.RegisterCallback<ChangeEvent<Object>>(_ =>
            {
                UpdateChosenGameConfig();
                UpdateLauncherBuildProblems();
            });

            inspectorTree.Q<Button>("manage-games-button").clicked += () =>
                ManageGamesInElympicsWindow.ShowWindow(serializedObject);

            UpdateSdkVersion();
            UpdateChosenGameConfig();
            UpdateLauncherBuildProblems();

            return inspectorTree;

            void UpdateLauncherBuildProblems()
            {
                var problems = Launcher.ElympicsLauncherBuildValidation.FindProblems(config);
                var warnings = Launcher.ElympicsLauncherBuildValidation.FindWarnings(config);
                var sections = new List<string>();
                if (problems.Count > 0)
                    sections.Add("Client builds will fail:\n- " + string.Join("\n- ", problems));
                if (warnings.Count > 0)
                    sections.Add("Warning:\n- " + string.Join("\n- ", warnings));
                launcherBuildProblems.text = string.Join("\n", sections);
                launcherBuildProblems.messageType = problems.Count > 0 ? HelpBoxMessageType.Error : HelpBoxMessageType.Warning;
                launcherBuildProblems.SetVisible(sections.Count > 0);
            }

            void UpdateSdkVersion()
            {
                sdkVersion.text = $"Elympics SDK version: {ElympicsConfig.SdkVersion}";
            }

            void UpdateChosenGameConfig()
            {
                chosenGameConfig.SetVisible(false);
                noGameConfigInfo.SetVisible(false);

                var gameConfig = config.GetCurrentGameConfig();
                if (gameConfig == null)
                {
                    noGameConfigInfo.SetVisible(true);
                    _ = CreateChosenGameEditorIfChanged(null);
                    return;
                }

                chosenGameConfig.SetVisible(true);
                gameTitle.text = gameConfig.GameName;

                if (!CreateChosenGameEditorIfChanged(gameConfig))
                    return;
                gameConfigNestingRoot.Clear();
                if (_currentGameConfig.HasValue)
                    gameConfigNestingRoot.Add(_currentGameConfig.Value.Inspector);
            }
        }

        private bool CreateChosenGameEditorIfChanged(ElympicsGameConfig? gameConfig)
        {
            if (gameConfig == null)
            {
                _currentGameConfig = null;
                return true;
            }

            if (_currentGameConfig != null && _currentGameConfig.Value.Reference == gameConfig)
                return false;

            var editor = CreateEditor(gameConfig);
            var element = editor.CreateInspectorGUI();
            element.Bind(editor.serializedObject);
            _currentGameConfig = (gameConfig, element);
            return true;
        }
    }
}
