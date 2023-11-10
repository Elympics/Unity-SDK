using UnityEngine;
using Elympics;
using System;
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
            var (Region, LatencyMs) = await ClosestRegionFinder.GetClosestRegion(); // TODO: use region
            await ElympicsLobbyClient.Instance.RoomsManager.CreateAndJoinRoom(roomViewElements.RoomName.text, queue, false, !roomViewElements.IsPublic, false);
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
