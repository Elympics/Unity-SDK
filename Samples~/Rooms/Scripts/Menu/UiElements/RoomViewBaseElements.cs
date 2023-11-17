using UnityEngine;
using TMPro;

public class RoomViewBaseElements : MonoBehaviour
{
    private static int PublicRoomOptionIndex = 0;
    private static int PrivateRoomOptionIndex = 1;

    [SerializeField] private TMP_InputField roomName;
    [SerializeField] private RadioButtonGroup roomPrivacy;
    [SerializeField] private TMP_InputField sampleGameData;

    public bool IsPrivate => roomPrivacy.CurrentOptionIndex == PrivateRoomOptionIndex;

    public TMP_InputField RoomName => roomName;
    public RadioButtonGroup RoomPrivacy => roomPrivacy;
    public TMP_InputField SampleGameData => sampleGameData; // TODO: Connect to the UI

    public void ManageInteractability(bool shouldBeInteractable)
    {
        roomName.interactable = shouldBeInteractable;
        roomPrivacy.ManageInteractability(shouldBeInteractable);
        sampleGameData.interactable = shouldBeInteractable;
    }

    public void SetPrivacy(bool isPrivate) => roomPrivacy.SelectOption(isPrivate ? PrivateRoomOptionIndex : PublicRoomOptionIndex);
}
