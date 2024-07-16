using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RadioButtonGroup : MonoBehaviour
{
    [SerializeField] private int defaultOptionIndex;
    [SerializeField] private UnityEvent OnOptionChanged;

    private List<RadioButtonOption> options;
    private bool interactable;

    private RadioButtonOption CurrentOption => options[CurrentOptionIndex];
    public int CurrentOptionIndex { get; private set; } = -1;
    public void PopulateOptions()
    {
        options = new(GetComponentsInChildren<RadioButtonOption>());
        foreach (var option in options)
            option.Init(this);

        Restart();
    }
    public void Restart()
    {
        interactable = true;

        CurrentOptionIndex = defaultOptionIndex;
        CurrentOption.ReactToSelection(true);
    }

    public void SelectOption(int chosenOptionIndex)
    {
        if (chosenOptionIndex == CurrentOptionIndex)
            return;

        CurrentOption.ReactToSelection(false, interactable);
        CurrentOptionIndex = chosenOptionIndex;
        CurrentOption.ReactToSelection(true);

        OnOptionChanged?.Invoke();
    }

    public void SelectOption(RadioButtonOption chosenOption) => SelectOption(options.IndexOf(chosenOption));

    public void ManageInteractability(bool shouldBeInteractable)
    {
        interactable = shouldBeInteractable;

        foreach (var option in options)
        {
            option.ManageInteractabilty(shouldBeInteractable);
        }

        CurrentOption.ReactToSelection(true);
    }
}
