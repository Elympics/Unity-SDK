using UnityEditor;
using UnityEngine;

#nullable enable

namespace SCS.Tests.Editor
{
    [CustomEditor(typeof(ScsClientTest))]
    public class ScsClientTestEditorWindow : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var mock = (ScsClientTest)target;
            _ = DrawDefaultInspector();

            if (GUILayout.Button("Get ChainConfig"))
                mock.GetChainconfigConfigFromSCS();
            if (GUILayout.Button("Get Ticket"))
                mock.GetTicketRequest();
            if (GUILayout.Button("Get and sign dummy ticket"))
                mock.GetAndSignTicketRequest();
            if (GUILayout.Button("On Player ready flow"))
                mock.ScsPlayerReadyFlow();

            if (GUILayout.Button($"Set Happy Path for room ready to {!SmartContractServiceMockSetup.HappyPathForMarkPlayerReady}"))
                SmartContractServiceMockSetup.ToggleHappyPathForMarkPlayerReady();
        }
    }
}
