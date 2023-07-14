using Elympics;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ChangeSelectedGameButton : MonoBehaviour
{
    [SerializeField]
    private Button button;

    [SerializeField]
    private Text nameText;

    [SerializeField]
    [HideInInspector]
    private int linkedGameIndex;

    [SerializeField]
    [HideInInspector]
    private string linkedGameId;

    [SerializeField]
    [HideInInspector]
    private ElympicsConfig elympicsConfig;

    public string LinkedId => linkedGameId;

    public void LinkWithGame(int index, string id, string name, ElympicsConfig elympicsConfig)
    {
        this.elympicsConfig = elympicsConfig;
        linkedGameIndex = index;
        linkedGameId = id;
        nameText.text = name;
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(nameText);
#endif
    }

    public void OnClick()
    {
        if (elympicsConfig == null)
            elympicsConfig = ElympicsConfig.Load();
        elympicsConfig.SwitchGame(linkedGameIndex);
    }

    internal void SetInteractable(bool value)
    {
        button.interactable = value;
    }
}
