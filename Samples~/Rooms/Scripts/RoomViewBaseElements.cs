using UnityEngine;
using TMPro;

public class RoomViewBaseElements : MonoBehaviour
{
    private const int PublicRoomOptionIndex = 0;

    [SerializeField] private TextMeshProUGUI roomName;
    [SerializeField] private RadioButtonGroup roomPrivacy;
    [SerializeField] private TMP_InputField sampleGameData;

    public bool IsPublic => RoomPrivacy.CurrentOptionIndex == PublicRoomOptionIndex;

    public TextMeshProUGUI RoomName => roomName;
    public RadioButtonGroup RoomPrivacy => roomPrivacy;
    public TMP_InputField SampleGameData => sampleGameData; // TODO: Connect to the UI
}
