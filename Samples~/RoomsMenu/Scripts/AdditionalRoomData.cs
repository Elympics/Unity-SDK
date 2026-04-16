using System;
using System.Collections.Generic;
using Elympics;
using UnityEngine;

public class AdditionalRoomData : MonoBehaviour
{
    private Dictionary<string, string> _matchmakingRoomData = new();
    private Dictionary<string, string> _customRoomData = new();

    private Action<Dictionary<string, string>, Dictionary<string, string>> _onValuesChanged;
    private Action<string, string> _onMatchmakingAdd;
    private Action<string, string> _onRoomDataAdd;
    private Action<string> _onMatchmakingRemove;
    private Action<string> _onRoomDataRemove;
    private Action _onDataUpdate;

    [SerializeField] private AdditionalRoomDataUi ui;
    private void Awake()
    {
        _onMatchmakingAdd += AddMatchmakingData;
        _onMatchmakingAdd += (_, _) => ui.ClearMatchmakingInputField();
        _onRoomDataAdd += AddRoomData;
        _onRoomDataAdd += (_, _) => ui.ClearRoomDataInputFields();

        _onMatchmakingRemove += RemoveMatchmakingData;
        _onMatchmakingRemove += (_) => ui.ClearMatchmakingInputField();
        _onRoomDataRemove += RemoveRoomData;
        _onRoomDataRemove += (_) => ui.ClearRoomDataInputFields();

        _onValuesChanged += ui.SetValues;
        ui.Init(_onMatchmakingAdd, _onRoomDataAdd, _onMatchmakingRemove, _onRoomDataRemove);
    }
    private void OnDestroy()
    {
        ui.Deinit();
        _onValuesChanged = null;
        _onRoomDataRemove = null;
        _onMatchmakingRemove = null;
        _onRoomDataAdd = null;
        _onMatchmakingAdd = null;
    }
    public void SubscribeOnDataUpdate(Action onUpdate) => _onDataUpdate = onUpdate;
    public void UnsubscribeOnDataUpdate() => _onDataUpdate = null;
    private void AddMatchmakingData(string key, string value)
    {
        if (_matchmakingRoomData.ContainsKey(key))
            _matchmakingRoomData[key] = value;
        else
            _matchmakingRoomData.Add(key, value);

        _onValuesChanged?.Invoke(_matchmakingRoomData, _customRoomData);
        _onDataUpdate?.Invoke();
    }
    private void AddRoomData(string key, string value)
    {
        if (_customRoomData.ContainsKey(key))
            _customRoomData[key] = value;
        else
            _customRoomData.Add(key, value);

        _onValuesChanged?.Invoke(_matchmakingRoomData, _customRoomData);
        _onDataUpdate?.Invoke();
    }
    private void RemoveMatchmakingData(string key)
    {
        var removed = _matchmakingRoomData.Remove(key);
        if (removed)
        {
            _onValuesChanged?.Invoke(_matchmakingRoomData, _customRoomData);
            _onDataUpdate?.Invoke();
        }
    }
    private void RemoveRoomData(string key)
    {
        var removed = _customRoomData.Remove(key);
        if (removed)
        {
            _onValuesChanged?.Invoke(_matchmakingRoomData, _customRoomData);
            _onDataUpdate?.Invoke();
        }
    }
    public void PopulateData(IRoom currentRoom)
    {
        var matchmakingData = currentRoom.State.MatchmakingData.CustomData;
        var roomData = currentRoom.State.CustomData;
        foreach (var entry in matchmakingData)
        {
            AddRoomData(entry.Key, entry.Value);
        }
        foreach (var entry in roomData)
        {
            AddMatchmakingData(entry.Key, entry.Value);
        }
    }
    public void UpdateUi()
    {
        ui.PopulateMatchmakingDropdown(_matchmakingRoomData);
        ui.PopulateRoomDropdown(_customRoomData);
    }
    public void ClearData()
    {
        _matchmakingRoomData.Clear();
        _customRoomData.Clear();
        ui.ClearMatchmakingInputField();
        ui.ClearRoomDataInputFields();
        _onValuesChanged?.Invoke(_matchmakingRoomData, _customRoomData);
    }
    public void Load()//TODO
    {
        if (PlayerPrefs.HasKey("MatchmakingData"))
        {
        }
        else
        {
        }
        if (PlayerPrefs.HasKey("RoomData"))
        {
        }
        else
        {
        }
    }
    public AdditionalRoomDataUi GetDataHolderUi() => ui;
    public Dictionary<string, string> GetMatchmakingRoomData() => _matchmakingRoomData;
    public Dictionary<string, string> GetCustomRoomData() => _customRoomData;
}
