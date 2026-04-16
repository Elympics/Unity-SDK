using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDropdownManager : MonoBehaviour
{
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Button removeItemButton;
    [SerializeField, HideInInspector] private AdditionalRoomDataUi _additionalRoomDataUi;
    private Action<string> _removeEntryClicked;
    [SerializeField, HideInInspector] private bool _matchmaking = false;

    public void Init(bool matchmaking, AdditionalRoomDataUi roomDataHolderUi)
    {
        _matchmaking = matchmaking;
        _additionalRoomDataUi = roomDataHolderUi;
    }
    public void Deinit()
    {
        _additionalRoomDataUi = null;
    }
    private void OnEnable()
    {
        if (_matchmaking)
            _removeEntryClicked += _additionalRoomDataUi.GetMatchmakingRemoveAction();
        else
            _removeEntryClicked += _additionalRoomDataUi.GetRoomDataRemoveAction();
        removeItemButton.onClick.AddListener(OnEntryRemoveClick);
    }
    private void OnDisable()
    {
        _removeEntryClicked = null;
    }
    private void OnEntryRemoveClick()
    {
        var key = AdditionalRoomDataUtilities.ExtractKeyValue(labelText.text);
        _removeEntryClicked?.Invoke(key);
    }
}
