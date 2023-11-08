using UnityEngine;
using TMPro;

public class CopyUtility : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textField;

    public void Copy()
    {
        GUIUtility.systemCopyBuffer = textField.text;
    }
}
