using System;
using Elympics;
using JetBrains.Annotations;
using UnityEngine;
using TMPro;

public class JoinWithCodePopupController : BasePopup
{
    [SerializeField] private TextMeshProUGUI roomNameTextField;

    private IRoom room;
    private Action<IRoom, BasePopup> joinRoomAction;

    public void Init(Action<IRoom, BasePopup> joinRoomAction)
    {
        this.joinRoomAction = joinRoomAction;
    }

    public void SetAndShow(IRoom room)
    {
        this.room = room;
        roomNameTextField.text = room?.State?.RoomName;

        Show();
    }

    public override void Hide()
    {
        base.Hide();
        room = null;
    }

    [UsedImplicitly]
    public void TryJoinRoomByCode()
    {
        joinRoomAction?.Invoke(room, this);
    }
}
