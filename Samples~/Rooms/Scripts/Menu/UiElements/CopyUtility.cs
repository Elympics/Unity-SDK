using TMPro;
using UnityEngine;

public class CopyUtility : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textField;

    public void Copy()
    {
        GUIUtility.systemCopyBuffer = textField.text;
    }
}
