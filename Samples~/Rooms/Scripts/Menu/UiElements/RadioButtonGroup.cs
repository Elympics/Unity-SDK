using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RadioButtonGroup : MonoBehaviour
{
    [SerializeField] private int defaultOptionIndex;
    [SerializeField] private UnityEvent OnOptionChanged;

    private List<RadioButtonOption> options;

    private RadioButtonOption CurrentOption => options[CurrentOptionIndex];
    public int CurrentOptionIndex { get; private set; } = -1;

    private void Awake()
    {
        options = new(GetComponentsInChildren<RadioButtonOption>());
        foreach (var option in options)
            option.Init(this);

        Restart();
    }

    public void Restart()
    {
        CurrentOptionIndex = defaultOptionIndex;
        CurrentOption.ReactToSelection(true);
    }

    public void SelectOption(int chosenOptionIndex)
    {
        if (chosenOptionIndex == CurrentOptionIndex)
            return;

        CurrentOption.ReactToSelection(false);
        CurrentOptionIndex = chosenOptionIndex;
        CurrentOption.ReactToSelection(true);

        OnOptionChanged?.Invoke();
    }

    public void SelectOption(RadioButtonOption chosenOption) => SelectOption(options.IndexOf(chosenOption));

    public void ManageInteractability(bool shouldBeInteractable)
    {
        foreach (var option in options)
        {
            option.ManageInteractabilty(shouldBeInteractable);
        }

        CurrentOption.ReactToSelection(true);
    }
}
