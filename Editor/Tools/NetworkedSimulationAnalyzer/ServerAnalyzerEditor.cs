using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.Collections.Generic;
using Plugins.Elympics.Plugins.ParrelSync;
using System;

namespace Elympics
{
    public class ServerAnalyzerEditor : EditorWindow, ITickAnalysis
    {
        private const string MenuPath = "Tools/Elympics/Networked Simulation Analyzer";
        private const string EditorTitle = "Networked Simulation Analyzer";
        private static readonly Vector2 MinWindowSize = new Vector2Int(660, 400);

        [SerializeField] private VisualTreeAsset uxml = null;
        [SerializeField] private VisualTreeAsset tickEntryTemplate = null;
        [SerializeField] private VisualTreeAsset inputDataTemplate = null;

        private TickListDisplayer _tickListDisplayer = null;
        private TickDataDisplayer _tickDataDisplayer = null;
        private ServerAnalyzerController _serverAnalyzerController = null;

        // TODO: find better way because its a bit dirty (maybe use change event from ElympicsConfig)
        private bool closedForcefully = false;

        public bool Paused => _serverAnalyzerController?.Paused ?? false;


        [MenuItem(MenuPath)]
        public static void OpenWindow()
        {
            var window = GetWindow<ServerAnalyzerEditor>();
            window.minSize = MinWindowSize;
            window.titleContent = new GUIContent(EditorTitle);
        }

        [MenuItem(MenuPath, true)]
        public static bool ValidateOpenWindow()
        {
            return !(ElympicsClonesManager.IsClone()
                || ElympicsConfig.LoadCurrentElympicsGameConfig().GameplaySceneDebugMode == ElympicsGameConfig.GameplaySceneDebugModeEnum.DebugOnlinePlayer);
        }

        private void OnDisable()
        {
            if (closedForcefully)
                return;

            ElympicsBase.TickAnalysis = null;

            if (ElympicsConfig.LoadCurrentElympicsGameConfig().GameplaySceneDebugMode == ElympicsGameConfig.GameplaySceneDebugModeEnum.DebugOnlinePlayer)
            {
                Debug.LogWarning("You cannot use the tool in the \"DebugOnlinePlayer\" mode!");
                closedForcefully = true;
                Close();
            }
        }

        private void OnEnable()
        {
            closedForcefully = false;

            uxml.CloneTree(rootVisualElement);

            _serverAnalyzerController = new ServerAnalyzerController(rootVisualElement);
            _tickDataDisplayer = new TickDataDisplayer(rootVisualElement, inputDataTemplate, _serverAnalyzerController);
            _tickListDisplayer = new TickListDisplayer(rootVisualElement, tickEntryTemplate, _tickDataDisplayer);

            _serverAnalyzerController.Initialize(_tickListDisplayer, _tickDataDisplayer);

            ElympicsBase.TickAnalysis = this;
        }

        public void Attach(Action<ElympicsSnapshot> snapshotApplier, bool[] isBots)
        {
            _serverAnalyzerController.SetSnapshotApplier(snapshotApplier);

            _tickDataDisplayer?.InitializeInputs(isBots ?? new bool[0]);

            var config = ElympicsConfig.LoadCurrentElympicsGameConfig();
            ServerAnalyzerUtils.ExpectedTime = 1000 * config.TickDuration;
        }

        public void Detach()
        {
            _serverAnalyzerController.OnExit();
        }

        public void AddSnapshotToAnalysis(ElympicsSnapshotWithMetadata snapshot, List<ElympicsSnapshotWithMetadata> reconciliationSnapshots, ClientTickCalculatorNetworkDetails networkDetails)
        {
            _tickListDisplayer.AddTick(snapshot);
        }
    }
}
