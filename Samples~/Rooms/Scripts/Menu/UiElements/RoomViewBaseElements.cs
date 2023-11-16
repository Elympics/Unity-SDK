using UnityEngine;
using TMPro;

public class RoomViewBaseElements : MonoBehaviour
{
    private const int PublicRoomOptionIndex = 0;

    [SerializeField] private TMP_InputField roomName;
    [SerializeField] private RadioButtonGroup roomPrivacy;
    [SerializeField] private TMP_InputField sampleGameData;

    public bool IsPublic => RoomPrivacy.CurrentOptionIndex == PublicRoomOptionIndex;

    public TMP_InputField RoomName => roomName;
    public RadioButtonGroup RoomPrivacy => roomPrivacy;
    public TMP_InputField SampleGameData => sampleGameData; // TODO: Connect to the UI

    public void ManageInteractability(bool shouldBeInteractable)
    {
        roomName.interactable = shouldBeInteractable;
        roomPrivacy.ManageInteractability(shouldBeInteractable);
        sampleGameData.interactable = shouldBeInteractable;
    }
}
