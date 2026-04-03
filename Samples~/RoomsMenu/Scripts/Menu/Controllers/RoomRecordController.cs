using System;
using Elympics;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomRecordController : MonoBehaviour
{
    [SerializeField] private Image backgroundImg;
    [SerializeField] private TextMeshProUGUI roomNameTextField;
    [SerializeField] private TextMeshProUGUI additionalRoomDataTextField;
    [SerializeField] private StateDependentButton joinButton;

    private IRoom _room;
    private Action<Guid> _joinRoomByIdAction;
    private Action<string, Guid> _setAndShowJoinCodePopupAction;

    public void Init(IRoom room, Action<Guid> joinRoomByIdAction, Action<string, Guid> setAndShowJoinCodePopupAction)
    {
        _room = room;
        _joinRoomByIdAction = joinRoomByIdAction;
        _setAndShowJoinCodePopupAction = setAndShowJoinCodePopupAction;

        Reset();
    }

    public void Reset()
    {
        roomNameTextField.text = _room.State.RoomName;

        if (_room.State.MatchmakingData.CustomData.TryGetValue(RoomsUtility.SampleMatchmakingDataKey, out var value))
            additionalRoomDataTextField.text = value;
        else
            Debug.LogWarning($"Room {_room.RoomId} has no CustomMatchmakingData of key {RoomsUtility.SampleMatchmakingDataKey}");

        SetJoinButtonState();
    }
    public void SetBackgroundColor(Color newColor) => backgroundImg.color = newColor;
    public void SetJoinButtonState()
    {
        JoinButtonState joinButtonState;

        joinButtonState = _room.State.Users.Count == RoomsUtility.RoomCapacity(_room) ? JoinButtonState.Full :
            _room.State.IsPrivate ? JoinButtonState.JoinPrivate :
                JoinButtonState.JoinPublic;

        joinButton.SetState((int)joinButtonState);
    }

    [UsedImplicitly]
    public void JoinRoom() => _joinRoomByIdAction?.Invoke(_room.RoomId);

    [UsedImplicitly]
    public void ShowJoinCodePopup() => _setAndShowJoinCodePopupAction?.Invoke(_room.State.RoomName, _room.RoomId);
}

public enum JoinButtonState { JoinPublic, JoinPrivate, Full }
