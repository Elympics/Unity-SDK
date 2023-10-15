using UnityEditor;
using UnityEngine;

namespace Elympics
{
    [InitializeOnLoad]
    internal class Mockings
    {
        public const string MockActivationKey = "MocksActive";
        static Mockings()
        {
            if (PlayerPrefs.GetInt(MockActivationKey) == 0)
            {
                ElympicsLobbyClient.AuthClientOverride = null;
                ElympicsLobbyClient.WebSocketFactoryOverride = null;
                return;
            }

            ElympicsLobbyClient.AuthClientOverride = WebSocketMockSetup.MockAuthClient();
            ElympicsLobbyClient.WebSocketFactoryOverride = WebSocketMockSetup.MockWebSocketFactory;
        }
    }
}
