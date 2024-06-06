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

    private Action<string, Guid?> _joinRoomWithCodeAction;
    private Guid? _relatedRoomId = null;

    public BasePopup PopupView => popupView;

    public void Init(Action<string, Guid?> joinRoomWithCodeAction) => _joinRoomWithCodeAction = joinRoomWithCodeAction;
    public void Deinit() => _joinRoomWithCodeAction = null;
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
    public void TryJoinRoomByCode() => _joinRoomWithCodeAction?.Invoke(joinCodeField.text, _relatedRoomId);
}
