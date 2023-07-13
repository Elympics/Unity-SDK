using System;
using System.Collections.Generic;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Elympics
{
    public class ServerAnalyzerEditor : EditorWindow, ITickAnalysis
    {
        private const string MenuPath = "Tools/Elympics/Networked Simulation Analyzer";
        private const string EditorTitle = "Networked Simulation Analyzer";
        private static readonly Vector2 MinWindowSize = new Vector2Int(660, 400);

        [SerializeField] private VisualTreeAsset uxml;
        [SerializeField] private VisualTreeAsset tickEntryTemplate;
        [SerializeField] private VisualTreeAsset inputDataTemplate;

        private TickListDisplayer _tickListDisplayer;
        private TickDataDisplayer _tickDataDisplayer;
        private ServerAnalyzerController _serverAnalyzerController;

        // TODO: find better way because its a bit dirty (maybe use change event from ElympicsConfig)
        private bool closedForcefully;

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

            _tickDataDisplayer?.InitializeInputs(isBots ?? Array.Empty<bool>());

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
