using Elympics.SnapshotAnalysis;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Elympics.Editor.Replay
{
    [InitializeOnLoad]
    public class ReplayControllerWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Elympics/" + EditorTitle;
        private const string EditorTitle = "Replay Controller";
        private static readonly Vector2 MinWindowSize = new Vector2Int(660, 400);

        public IReplayManipulator ReplayManipulator => _replayController;

        [SerializeField] private VisualTreeAsset uxml;
        [SerializeField] private VisualTreeAsset tickEntryTemplate;
        [SerializeField] private VisualTreeAsset inputDataTemplate;

        private TickListDisplayer _tickListDisplayer;
        private TickDataDisplayer _tickDataDisplayer;
        private EditorReplayController _replayController;

        static ReplayControllerWindow() => EditorSnapshotReplayInitializer.GetManipulator = () => GetWindow<ReplayControllerWindow>().ReplayManipulator;

        [MenuItem(MenuPath)]
        public static void OpenWindow() => _ = GetWindow<ReplayControllerWindow>();

        private void OnEnable()
        {
            minSize = MinWindowSize;
            titleContent = new GUIContent(EditorTitle);

            uxml.CloneTree(rootVisualElement);

            _tickDataDisplayer = new TickDataDisplayer(rootVisualElement, inputDataTemplate);
            _tickListDisplayer = new TickListDisplayer(rootVisualElement, tickEntryTemplate, _tickDataDisplayer);
            _replayController = new EditorReplayController(rootVisualElement, _tickListDisplayer, _tickDataDisplayer);
        }
    }
}
