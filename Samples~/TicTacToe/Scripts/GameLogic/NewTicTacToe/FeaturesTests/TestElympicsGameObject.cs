using Elympics;
using UnityEngine;

public class TestElympicsGameObject : ElympicsMonoBehaviour, IUpdatable, IInitializable
{
    [SerializeField] private ElympicsBehaviour singleGameObjectReference;
    [SerializeField] private ElympicsBehaviour[] multipleGameObjectReferences;

    private readonly ElympicsInt _tick = new();

    private readonly ElympicsGameObject _singleElympicsGameObject = new();
    private readonly ElympicsList<ElympicsGameObject> _listWithElympicsGameObjects = new(() => new ElympicsGameObject());

    public void Initialize()
    {
        foreach (var behaviour in multipleGameObjectReferences)
            _listWithElympicsGameObjects.Add().Value = behaviour;
    }

    public void ElympicsUpdate()
    {
        _tick.Value++;

        if (_tick.Value % 200 == 80)
        {
            ChangeSingleGameObject();
            ChangePositionsInList();
        }
    }

    private void ChangePositionsInList()
    {
        var firstElement = _listWithElympicsGameObjects[0].Value;

        for (var i = 1; i < _listWithElympicsGameObjects.Count; i++)
            _listWithElympicsGameObjects[i - 1].Value = _listWithElympicsGameObjects[i].Value;

        _listWithElympicsGameObjects[^1].Value = firstElement;
    }

    private void ChangeSingleGameObject()
    {
        _singleElympicsGameObject.Value = _singleElympicsGameObject.Value == null ? singleGameObjectReference : null;
    }
}
