using TMPro;
using UnityEngine;

public class PasteUtility : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;

    public void Paste()
    {
        inputField.text = GUIUtility.systemCopyBuffer;
    }
}
