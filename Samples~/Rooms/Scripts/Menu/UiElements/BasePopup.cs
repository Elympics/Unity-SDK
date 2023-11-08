using UnityEngine;
using TMPro;

public class BasePopup : BaseWindow
{
    [SerializeField] private TextMeshProUGUI logTextField;

    public void LogText(string text)
    {
        logTextField.text = text;
        Show();
    }

    public override void Hide()
    {
        base.Hide();

        logTextField.text = string.Empty;
    }
}
