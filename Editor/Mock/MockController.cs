using UnityEditor;
using UnityEngine;

namespace Elympics
{
    [InitializeOnLoad]
    internal class MockController //TODO create separate assembly for MockController
    {
        public const string MockActivationKey = "MocksActive";
        static MockController()
        {
            if (PlayerPrefs.GetInt(MockActivationKey) == 0)
            {
                ElympicsLobbyClient.AuthClientOverride = null;
                ElympicsLobbyClient.WebSocketFactoryOverride = null;
                return;
            }

            ElympicsLobbyClient.AuthClientOverride = WebSocketMockSetup.MockAuthClient();
            ElympicsLobbyClient.WebSocketFactoryOverride = WebSocketMockSetup.MockWebSocketFactory;
            RespectService.WebRequestOverride = RespectServiceWebRequestMock.MockRespectServiceWebRequest();
        }
    }
}
