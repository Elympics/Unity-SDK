using UnityEngine;
using TMPro;

public class PasteUtility : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;

    public void Paste()
    {
        inputField.text = GUIUtility.systemCopyBuffer;
    }
}