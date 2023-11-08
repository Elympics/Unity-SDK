using UnityEngine;
using TMPro;
using Elympics;
using System;
using JetBrains.Annotations;

public class RoomRecordController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomNameTextField;
    [SerializeField] private TextMeshProUGUI additionalRoomDataTextField;
    [SerializeField] private StateDependentButton joinButton;

    private IRoom room;
    private Action<IRoom> joinRoomAction;
    private Action<IRoom> setAndShowJoinCodePopupAction;

    public void Init(IRoom room, Action<IRoom> joinRoomAction, Action<IRoom> setAndShowJoinCodePopupAction)
    {
        this.room = room;
        this.joinRoomAction = joinRoomAction;
        this.setAndShowJoinCodePopupAction = setAndShowJoinCodePopupAction;

        roomNameTextField.text = room.RoomName;
        additionalRoomDataTextField.text = room?.State.RoomParameters[RoomsUtility.SampleDataKey];
        SetJoinButtonState();
    }

    public void SetJoinButtonState()
    {
        // TODO: Use privacy parameter instead
        JoinButtonState joinButtonState;
        if (room.State.Users.Count == RoomsUtility.MaxPlayers)
            joinButtonState = JoinButtonState.Full;
        else if (room.JoinCode != null)
            joinButtonState = JoinButtonState.JoinPrivate;
        else
            joinButtonState = JoinButtonState.JoinPublic;

        joinButton.SetState((int)joinButtonState);
    }

    [UsedImplicitly]
    public void JoinRoom() => joinRoomAction?.Invoke(room);

    [UsedImplicitly]
    public void ShowJoinCodePopup() => setAndShowJoinCodePopupAction?.Invoke(room);
}

public enum JoinButtonState { JoinPublic, JoinPrivate, Full }
