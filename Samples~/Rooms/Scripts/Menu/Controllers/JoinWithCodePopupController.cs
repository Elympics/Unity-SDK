using System;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinWithCodePopupController : MonoBehaviour
{
    [SerializeField] private BasePopup popupView;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField joinCodeField;

    private Action<string, Guid?> joinRoomWithCodeAction;
    private Guid? relatedRoomId = null;


    public BasePopup PopupView => popupView;

    public void Init(Action<string, Guid?> joinRoomWithCodeAction)
    {
        this.joinRoomWithCodeAction = joinRoomWithCodeAction;
    }

    public void SetAndShow(string roomName, Guid roomId)
    {
        popupView.SetTitle(roomName);
        relatedRoomId = roomId;

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

        relatedRoomId = null;
        popupView.Reset();
    }

    [UsedImplicitly]
    public void TryJoinRoomByCode()
    {
        Debug.Log("Attempting to log by code");
        joinRoomWithCodeAction?.Invoke(joinCodeField.text, relatedRoomId);
    }
}
