using UnityEngine;
using TMPro;
using Elympics;
using System;
using JetBrains.Annotations;
using Elympics.Rooms.Models;

public class RoomRecordController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomNameTextField;
    [SerializeField] private TextMeshProUGUI additionalRoomDataTextField;
    [SerializeField] private StateDependentButton joinButton;

    private RoomStateChanged room;
    private Action<Guid> joinRoomByIdAction;
    private Action<string> setAndShowJoinCodePopupAction;

    public void Init(RoomStateChanged room, Action<Guid> joinRoomByIdAction, Action<string> setAndShowJoinCodePopupAction)
    {
        this.room = room;
        this.joinRoomByIdAction = joinRoomByIdAction;
        this.setAndShowJoinCodePopupAction = setAndShowJoinCodePopupAction;

        roomNameTextField.text = room.RoomName;
        additionalRoomDataTextField.text = room.MatchmakingData.CustomData[RoomsUtility.SampleDataKey];
        SetJoinButtonState();
    }

    public void SetJoinButtonState()
    {
        JoinButtonState joinButtonState;
        if (room.Users.Count == RoomsUtility.MaxPlayers)
            joinButtonState = JoinButtonState.Full;
        else if (room.IsPrivate)
            joinButtonState = JoinButtonState.JoinPrivate;
        else
            joinButtonState = JoinButtonState.JoinPublic;

        joinButton.SetState((int)joinButtonState);
    }

    [UsedImplicitly]
    public void JoinRoom() => joinRoomByIdAction?.Invoke(room.RoomId);

    [UsedImplicitly]
    public void ShowJoinCodePopup() => setAndShowJoinCodePopupAction?.Invoke(room.RoomName);
}

public enum JoinButtonState { JoinPublic, JoinPrivate, Full }
