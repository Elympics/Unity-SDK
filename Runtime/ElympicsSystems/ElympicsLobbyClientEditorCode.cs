#if UNITY_EDITOR
using Elympics.Models.Authentication;
using UnityEditor;

namespace Elympics
{
    public partial class ElympicsLobbyClient
    {
        private void OnValidate()
        {
            if (PrefabUtility.IsPartOfPrefabAsset(this) || migratedAuthSettings)
                return;
            Undo.RecordObject(this, $"Migrate auth settings from {nameof(ElympicsLobbyClient)}");
            if (!authenticateOnAwake)
                authenticateOnAwakeWith = AuthType.None;
            migratedAuthSettings = true;
            if (PrefabUtility.IsPartOfPrefabInstance(this))
                PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }
    }
}
#endif
