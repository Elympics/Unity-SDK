using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomCreationView : RoomViewBaseElements
{
    [SerializeField] private TMP_Dropdown teamSizes;
    [SerializeField] private Button backButton;
    [SerializeField] private Button createButton;
    public override void Init(AdditionalRoomDataUi additionalRoomDataUi)
    {
        base.Init(additionalRoomDataUi);
        PopulateTeamSizeDropdown();
    }
    private void PopulateTeamSizeDropdown()
    {
        if (teamSizes == null)
            return;
        teamSizes.ClearOptions();
        for (var i = 0; i < RoomsUtility.QueueTypes.Length; i++)
        {
            teamSizes.options.Add(new TMP_Dropdown.OptionData(RoomsUtility.QueueTypes[i]));
        }
        teamSizes.RefreshShownValue();
    }
    public string GetSelectedGameMode() => teamSizes.options[teamSizes.value].text;

    public void ManageWindowInteractability(bool interactable)
    {
        createButton.interactable = interactable;
        backButton.interactable = interactable;
    }
}
