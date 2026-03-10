using TMPro;
using UnityEngine;

public class RoomViewBaseElements : MonoBehaviour
{
    private static readonly int PublicRoomOptionIndex = 0;
    private static readonly int PrivateRoomOptionIndex = 1;

    [SerializeField] private TMP_InputField roomName;
    [SerializeField] private RadioButtonGroup roomPrivacy;
    private AdditionalRoomDataUi _additionalRoomDataUi;
    public bool IsPrivate => roomPrivacy.CurrentOptionIndex == PrivateRoomOptionIndex;

    public TMP_InputField RoomName => roomName;
    public RadioButtonGroup RoomPrivacy => roomPrivacy;

    public virtual void Init(AdditionalRoomDataUi additionalRoomDataUi)
    {
        _additionalRoomDataUi = additionalRoomDataUi;
        RoomPrivacy.PopulateOptions();
    }
    public void ManageInteractability(bool shouldBeInteractable)
    {
        roomName.interactable = shouldBeInteractable;
        roomPrivacy.ManageInteractability(shouldBeInteractable);
        _additionalRoomDataUi.ManageInteractability(shouldBeInteractable);
    }
    public void SetPrivacy(bool isPrivate) => roomPrivacy.SelectOption(isPrivate ? PrivateRoomOptionIndex : PublicRoomOptionIndex);
}
