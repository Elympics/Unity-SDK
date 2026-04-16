using System;
using UnityEngine;

public class RoomWindowView : RoomViewBaseElements
{

    [SerializeField] private Transform seatsHolder;
    [SerializeField] private PlayerSeat playerSeatPrefab;
    public PlayerSeat GetPlayerSeat(string seatName = "PlayerSeat")
    {
        var seat = Instantiate(playerSeatPrefab);
        seat.name = seatName;
        return seat;
    }
    public Transform GetSeatsHolder() => seatsHolder;

    public void SubscribeRoomNameChenge(Action onRoomNameChange)
    {
        RoomName.onEndEdit.AddListener((_) => onRoomNameChange?.Invoke());
    }
    public void UnsubscribeRoomNameChanged()
    {
        RoomName.onEndEdit.RemoveAllListeners();
    }
}
