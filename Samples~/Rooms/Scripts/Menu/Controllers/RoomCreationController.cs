using UnityEngine;
using Elympics;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

public class RoomCreationController : BaseWindow
{
    [SerializeField] private RoomViewBaseElements roomViewElements;
    [SerializeField] private string queue;

    [UsedImplicitly]
    public async void CreateAndJoinRoom()
    {
        try
        {
            var room = await RoomsUtility.RoomsManager.CreateAndJoinRoom(roomViewElements.RoomName.text, queue, false, roomViewElements.IsPrivate, new Dictionary<string, string>(), new Dictionary<string, string>());
            await room.UpdateRoomParams(null, null, null, new (){ { ":pub:SampleData", roomViewElements.SampleGameData.text }, { "ExamplePrivateData", "test private data" } });
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
        return $"{UnityEngine.Random.Range(0, 10000)}'s room";
    }

    [UsedImplicitly]
    public void ShowRoomChoiceView() => RoomsNavigationController.Instance.ShowRoomChoiceView();
}
