using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class RoomCreationController : BaseWindow
{
    [SerializeField] private RoomViewBaseElements roomViewElements;
    [SerializeField] private string queue;

    [UsedImplicitly]
    public async void CreateAndJoinRoom()
    {
        try
        {
            _ = await RoomsUtility.RoomsManager.CreateAndJoinRoom(roomViewElements.RoomName.text, queue, false, roomViewElements.IsPrivate, new Dictionary<string, string>(), NewCustomMatchmakingData());
        }
        catch (Exception e)
        {
            Debug.LogError($"Room creation failed: {e.Message}");
        }
    }

    public override void Show()
    {
        base.Show();

        roomViewElements.RoomName.text = GenerateRandomRoomName();
        roomViewElements.RoomPrivacy.Restart();
        roomViewElements.SampleGameData.text = string.Empty;
    }

    private string GenerateRandomRoomName()
    {
        return $"Room {UnityEngine.Random.Range(0, 10000)}";
    }

    private Dictionary<string, string> NewCustomMatchmakingData() => new() { { ":pub:scs:bet", roomViewElements.SampleGameData.text }, { "ExamplePrivateData", "test private data" } };

    [UsedImplicitly]
    public void ShowRoomChoiceView() => RoomsNavigationController.Instance.ShowRoomChoiceView();
}
