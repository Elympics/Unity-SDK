using UnityEngine;
using TMPro;
using Elympics;

public class RoomViewBaseElements : MonoBehaviour
{
    private static readonly int PublicRoomOptionIndex = 0;
    private static readonly int PrivateRoomOptionIndex = 1;

    [SerializeField] private TMP_InputField roomName;
    [SerializeField] private RadioButtonGroup roomPrivacy;
    [SerializeField] private TMP_InputField sampleGameData;

    public bool IsPrivate => roomPrivacy.CurrentOptionIndex == PrivateRoomOptionIndex;

    public TMP_InputField RoomName => roomName;
    public RadioButtonGroup RoomPrivacy => roomPrivacy;
    public TMP_InputField SampleGameData => sampleGameData;

    public void ManageInteractability(bool shouldBeInteractable)
    {
        roomName.interactable = shouldBeInteractable;
        roomPrivacy.ManageInteractability(shouldBeInteractable);
        sampleGameData.interactable = shouldBeInteractable;
    }

    public void TrySetSampleData(IRoom room)
    {
        if (room.State.MatchmakingData.CustomData.TryGetValue(RoomsUtility.SampleDataKey, out string value))
            sampleGameData.text = value;
        else
            Debug.LogWarning($"Joined room has no CustomMatchmakingData of key {RoomsUtility.SampleDataKey}");
    }

    public void SetPrivacy(bool isPrivate) => roomPrivacy.SelectOption(isPrivate ? PrivateRoomOptionIndex : PublicRoomOptionIndex);
}
