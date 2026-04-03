using TMPro;
using UnityEngine;

public class BasePopup : BaseWindow
{
    [SerializeField] private TextMeshProUGUI titleTextField;
    [SerializeField] private TextMeshProUGUI logTextField;

    public void SetTitle(string title) => titleTextField.text = title;
    public void SetMessage(string message) => logTextField.text = message;

    public void Reset()
    {
        logTextField.text = string.Empty;
        titleTextField.text = string.Empty;
    }
}
