using UnityEngine;

namespace Elympics
{
    [CreateAssetMenu(fileName = "ManageGamesInElympicsWindowData", menuName = "ManageGamesInElympicsWindowData")]
    public class ManageGamesInElympicsWindowData : ScriptableObject
    {
        [SerializeField] internal Object objectToSerialize;
    }
}

