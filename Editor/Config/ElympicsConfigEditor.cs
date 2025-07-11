using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable

namespace Elympics.Editor
{
    [CustomEditor(typeof(ElympicsConfig))]
    public class ElympicsConfigEditor : UnityEditor.Editor
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

            availableGames.RegisterCallback<ChangeEvent<Object>>(_ => UpdateChosenGameConfig());

            inspectorTree.Q<Button>("manage-games-button").clicked += () =>
                ManageGamesInElympicsWindow.ShowWindow(serializedObject);

            UpdateSdkVersion();
            UpdateChosenGameConfig();

            return inspectorTree;

            void UpdateSdkVersion()
            {
                sdkVersion.text = $"Elympics SDK version: {ElympicsConfig.SdkVersion}";
            }

            void UpdateChosenGameConfig()
            {
                chosenGameConfig.style.display = DisplayStyle.None;
                noGameConfigInfo.style.display = DisplayStyle.None;

                var gameConfig = config.GetCurrentGameConfig();
                if (gameConfig == null)
                {
                    noGameConfigInfo.style.display = DisplayStyle.Flex;
                    _ = CreateChosenGameEditorIfChanged(null);
                    return;
                }

                chosenGameConfig.style.display = DisplayStyle.Flex;
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
