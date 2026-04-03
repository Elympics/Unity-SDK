using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdditionalRoomDataUi : MonoBehaviour
{
    [SerializeField] private Button addMatchmakingDataButton;
    [SerializeField] private Button addCustomRoomDataButton;
    [SerializeField] private TMP_InputField matchmakingDataKeyInputField;
    [SerializeField] private TMP_InputField matchmakingDataValueInputField;
    [SerializeField] private TMP_InputField roomDataKeyInputField;
    [SerializeField] private TMP_InputField roomDataValueInputField;
    [SerializeField] private TMP_Dropdown matchmakingDropdown;
    [SerializeField] private TMP_Dropdown roomDropdown;
    [SerializeField] private ItemDropdownManager matchmakingItemManager;
    [SerializeField] private ItemDropdownManager roomItemManager;

    private Action<string, string> onMatchmakingDataAddClicked;
    private Action<string> onMatchmakingDataRemoveClicked;
    private Action<string, string> onRoomDataAddClicked;
    private Action<string> onRoomDataRemoveClicked;
    public void Init(Action<string, string> onMatchmakingAdd, Action<string, string> onRoomAdd, Action<string> onMatchmakingRemove, Action<string> onRoomRemove)
    {
        onMatchmakingDataAddClicked += onMatchmakingAdd;
        onRoomDataAddClicked += onRoomAdd;

        addMatchmakingDataButton.onClick.AddListener(OnAddMatchmakingDataClicked);
        addCustomRoomDataButton.onClick.AddListener(OnAddRoomDataClicked);

        onMatchmakingDataRemoveClicked += onMatchmakingRemove;
        onRoomDataRemoveClicked += onRoomRemove;

        matchmakingItemManager.Init(true, this);
        roomItemManager.Init(false, this);
    }
    public void Deinit()
    {
        roomItemManager.Deinit();
        matchmakingItemManager.Deinit();

        onRoomDataRemoveClicked = null;
        onMatchmakingDataRemoveClicked = null;

        addCustomRoomDataButton.onClick.RemoveAllListeners();
        addMatchmakingDataButton.onClick.RemoveAllListeners();

        onRoomDataAddClicked = null;
        onMatchmakingDataAddClicked = null;
    }
    public void OnAddMatchmakingDataClicked()
    {
        if (string.IsNullOrEmpty(matchmakingDataKeyInputField.text) || string.IsNullOrEmpty(matchmakingDataValueInputField.text))
            return;
        onMatchmakingDataAddClicked?.Invoke(matchmakingDataKeyInputField.text, matchmakingDataValueInputField.text);
    }
    public void OnAddRoomDataClicked()
    {
        if (string.IsNullOrEmpty(roomDataKeyInputField.text) || string.IsNullOrEmpty(roomDataValueInputField.text))
            return;
        onRoomDataAddClicked?.Invoke(roomDataKeyInputField.text, roomDataValueInputField.text);
    }
    public void PopulateMatchmakingDropdown(Dictionary<string, string> values) => PopulateDropdown(matchmakingDropdown, values);
    public void PopulateRoomDropdown(Dictionary<string, string> values) => PopulateDropdown(roomDropdown, values);
    private void PopulateDropdown(TMP_Dropdown dropdown, Dictionary<string, string> values)
    {
        dropdown.options.Clear();
        var dropdownValue = new List<TMP_Dropdown.OptionData>();
        foreach (var entry in values)
        {
            var option = new TMP_Dropdown.OptionData($"Key: {entry.Key} Value: {entry.Value}");
            dropdownValue.Add(option);
        }
        dropdown.AddOptions(dropdownValue);
    }
    public void ClearMatchmakingDropdown() => ClearDropdown(matchmakingDropdown);
    public void ClearRoomDropdown() => ClearDropdown(roomDropdown);
    private void ClearInputFields(TMP_InputField inputField) => inputField.text = string.Empty;
    public void ClearMatchmakingInputField()
    {
        ClearInputFields(matchmakingDataKeyInputField);
        ClearInputFields(matchmakingDataValueInputField);
    }
    public void ClearRoomDataInputFields()
    {
        ClearInputFields(roomDataKeyInputField);
        ClearInputFields(roomDataValueInputField);
    }
    private void ClearDropdown(TMP_Dropdown dropdown)
    {
        dropdown.ClearOptions();
    }
    public void SetValues(Dictionary<string, string> matchmakingData, Dictionary<string, string> roomData)
    {
        matchmakingDropdown.Hide();
        roomDropdown.Hide();
        ClearDropdown(matchmakingDropdown);
        ClearDropdown(roomDropdown);
        foreach (var entry in matchmakingData)
        {
            var newItem = new TMP_Dropdown.OptionData(AdditionalRoomDataUtilities.GetDropdownEntry(entry.Key, entry.Value));
            matchmakingDropdown.options.Add(newItem);
        }
        foreach (var entry in roomData)
        {
            var newItem = new TMP_Dropdown.OptionData(AdditionalRoomDataUtilities.GetDropdownEntry(entry.Key, entry.Value));
            roomDropdown.options.Add(newItem);
        }
    }
    public void ManageInteractability(bool shouldBeInteractable)
    {
        matchmakingDataKeyInputField.interactable = shouldBeInteractable;
        matchmakingDataValueInputField.interactable = shouldBeInteractable;
        addMatchmakingDataButton.interactable = shouldBeInteractable;

        roomDataKeyInputField.interactable = shouldBeInteractable;
        roomDataValueInputField.interactable = shouldBeInteractable;
        addCustomRoomDataButton.interactable = shouldBeInteractable;

        matchmakingDropdown.interactable = shouldBeInteractable;
        roomDropdown.interactable = shouldBeInteractable;
    }
    public Action<string> GetMatchmakingRemoveAction() => onMatchmakingDataRemoveClicked;
    public Action<string> GetRoomDataRemoveAction() => onRoomDataRemoveClicked;

}
