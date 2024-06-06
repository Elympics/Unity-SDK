using System;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinWithIdPopupController : MonoBehaviour
{
    [SerializeField] private BasePopup popupView;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField joinCodeField;

    private Action<Guid?> _joinRoomWithIdAction;
    private Guid? _relatedRoomId = null;


    public BasePopup PopupView => popupView;

    public void Init(Action<Guid?> joinRoomWithIdAction) => _joinRoomWithIdAction = joinRoomWithIdAction;

    public void SetAndShow(string roomName, Guid roomId)
    {
        popupView.SetTitle(roomName);
        _relatedRoomId = roomId;

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

        _relatedRoomId = null;
        popupView.Reset();
    }

    [UsedImplicitly]
    public void TryJoinRoomById()
    {
        Debug.Log("Attempting to log by id");
        _relatedRoomId = Guid.TryParse(joinCodeField.text, out var guid) ? guid : null;

        _joinRoomWithIdAction?.Invoke(_relatedRoomId);
    }
}
