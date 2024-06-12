using UnityEditor;
using UnityEngine;

namespace SCS
{
    [InitializeOnLoad]
    public static class MockController
    {
        private const string MockActivationKey = "MocksActive";
        static MockController()
        {
            if (PlayerPrefs.GetInt(MockActivationKey) == 0)
            {
                SmartContractService.ScsWebRequestOverride = null;
                return;
            }

            SmartContractService.ScsWebRequestOverride = SmartContractServiceMockSetup.MockScsWebRequest();
        }
    }
}
