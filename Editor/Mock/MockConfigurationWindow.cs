using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Elympics
{
    public class MockConfigurationWindow : EditorWindow
    {
        private bool _mockActive;
        private const string MenuPath = "Tools/Elympics/Mock Configuration";
        private const string EditorTitle = "Mock Configuration";

        private static readonly Vector2 MinWindowSize = new Vector2Int(660, 400);
        private Button _mockToggleButton;
        private Label _mockStatusLabel;

        [MenuItem(MenuPath)]
        public static void OpenWindow()
        {
            var window = GetWindow<MockConfigurationWindow>();
            window.minSize = MinWindowSize;
            window.titleContent = new GUIContent(EditorTitle);
        }

        private void OnEnable() => _mockActive = PlayerPrefs.GetInt(MockController.MockActivationKey) != 0;

        public void CreateGUI()
        {
            var root = rootVisualElement;

            _mockStatusLabel = new Label();
            root.Add(_mockStatusLabel);

            _mockToggleButton = new Button();
            _mockToggleButton.name = _mockToggleButton.name = "Mocks Toggle";
            _mockToggleButton.clicked += OnMocksToggleButtonClicked;
            root.Add(_mockToggleButton);

            RefreshDescriptionTexts();
        }

        public void OnDisable()
        {
            if (_mockToggleButton != null)
                _mockToggleButton.clicked -= OnMocksToggleButtonClicked;
        }

        private void OnMocksToggleButtonClicked()
        {
            var newValue = _mockActive ? 0 : 1;
            PlayerPrefs.SetInt(MockController.MockActivationKey, newValue);
            _mockActive = IntToBool(newValue);
            RefreshDescriptionTexts();
        }

        private static bool IntToBool(int value) => value != 0;

        private void RefreshDescriptionTexts()
        {
            _mockToggleButton.text = _mockActive ? "Turn off Mocks" : "Turn on Mocks";
            _mockStatusLabel.text = _mockActive ? "Mocks Activated" : "Mocks Deactivated";
        }
    }
}
