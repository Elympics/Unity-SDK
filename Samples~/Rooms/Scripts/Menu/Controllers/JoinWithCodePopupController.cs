using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JoinWithCodePopupController : MonoBehaviour
{
    [SerializeField] private BasePopup popupView;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField joinCodeField;

    private Action<string> joinRoomWithCodeAction;

    public BasePopup PopupView => popupView;

    public void Init(Action<string> joinRoomWithCodeAction)
    {
        this.joinRoomWithCodeAction = joinRoomWithCodeAction;
    }

    public void SetAndShow(string roomName)
    {
        popupView.SetTitle(roomName);

        Show();
    }

    [UsedImplicitly]
    public void Show()
    {
        popupView.Show();

        joinCodeField.text = string.Empty;
        joinCodeField.ActivateInputField();
    }

    [UsedImplicitly]
    public void Hide()
    {
        popupView.Hide();

        popupView.Reset();
    }

    [UsedImplicitly]
    public void TryJoinRoomByCode()
    {
        Debug.Log("Attempting to log by code");
        joinRoomWithCodeAction?.Invoke(joinCodeField.text);
    }
}
