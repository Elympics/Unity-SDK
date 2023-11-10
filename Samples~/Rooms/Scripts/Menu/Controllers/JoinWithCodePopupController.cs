using System;
using Elympics;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

public class JoinWithCodePopupController : MonoBehaviour
{
    [SerializeField] private BasePopup popupView;
    [SerializeField] private Button joinButton;

    private IRoom room;
    private Action<IRoom, BasePopup> joinRoomAction;

    public void Init(Action<IRoom, BasePopup> joinRoomAction)
    {
        this.joinRoomAction = joinRoomAction;
    }

    public void SetAndShow(IRoom room)
    {
        this.room = room;

        popupView.SetTitle(room?.State?.RoomName);

        Show();
    }

    [UsedImplicitly]
    public void Show()
    {
        joinButton.interactable = true;

        popupView.Show();
    }

    [UsedImplicitly]
    public void Hide()
    {
        popupView.Hide();

        room = null;
        popupView.Reset();
    }

    [UsedImplicitly]
    public void TryJoinRoomByCode()
    {
        Debug.Log("Attempting to log by code");
        joinRoomAction?.Invoke(room, popupView);
        Debug.Log("Disabling join button");
        joinButton.interactable = false;
    }
}
