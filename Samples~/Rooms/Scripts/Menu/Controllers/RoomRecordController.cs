using System;
using Elympics;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

public class RoomRecordController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomNameTextField;
    [SerializeField] private TextMeshProUGUI additionalRoomDataTextField;
    [SerializeField] private StateDependentButton joinButton;

    private IRoom room;
    private Action<Guid> joinRoomByIdAction;
    private Action<string> setAndShowJoinCodePopupAction;

    public void Init(IRoom room, Action<Guid> joinRoomByIdAction, Action<string> setAndShowJoinCodePopupAction)
    {
        this.room = room;
        this.joinRoomByIdAction = joinRoomByIdAction;
        this.setAndShowJoinCodePopupAction = setAndShowJoinCodePopupAction;

        Reset();
    }

    public void Reset()
    {
        roomNameTextField.text = room.State.RoomName;

        if (room.State.MatchmakingData.CustomData.TryGetValue(RoomsUtility.SampleDataKey, out var value))
            additionalRoomDataTextField.text = value;
        else
            Debug.LogWarning($"Room {room.RoomId} has no CustomMatchmakingData of key {RoomsUtility.SampleDataKey}");

        SetJoinButtonState();
    }

    public void SetJoinButtonState()
    {
        JoinButtonState joinButtonState;
        if (room.State.Users.Count == RoomsUtility.RoomCapacity(room))
            joinButtonState = JoinButtonState.Full;
        else if (room.State.IsPrivate)
            joinButtonState = JoinButtonState.JoinPrivate;
        else
            joinButtonState = JoinButtonState.JoinPublic;

        joinButton.SetState((int)joinButtonState);
    }

    [UsedImplicitly]
    public void JoinRoom() => joinRoomByIdAction?.Invoke(room.RoomId);

    [UsedImplicitly]
    public void ShowJoinCodePopup() => setAndShowJoinCodePopupAction?.Invoke(room.State.RoomName);
}

public enum JoinButtonState { JoinPublic, JoinPrivate, Full }
