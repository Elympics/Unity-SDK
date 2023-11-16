using UnityEngine;
using UnityEngine.Events;

public class RadioButtonGroup : MonoBehaviour
{
    [SerializeField] private int defaultOptionIndex;
    [SerializeField] private UnityEvent OnOptionChanged;

    private RadioButtonOption[] options;

    public int CurrentOptionIndex { get; private set; }

    private void Awake()
    {
        options = GetComponentsInChildren<RadioButtonOption>();
        foreach (var option in options) option.SetUpOption(this);
        Restart();
    }

    public void Restart() => SelectOption(defaultOptionIndex);

    public void SelectOption(int chosenOptionIndex) => SelectOption(options[chosenOptionIndex]);

    public void SelectOption(RadioButtonOption chosenOption)
    {
        var previousOption = CurrentOptionIndex;

        for (int i = 0; i < options.Length; i++)
        {
            bool isChosenOption = options[i] == chosenOption;
            if (isChosenOption) CurrentOptionIndex = i;
            options[i].SetOptionState(isChosenOption ? RadioButtonStates.Selected : RadioButtonStates.Selectable);
        }

        if (previousOption != CurrentOptionIndex)
            OnOptionChanged?.Invoke();
    }

    public void ManageInteractability(bool shouldBeInteractable)
    {
        foreach (var option in options)
        {
            option.ManageInteractability(shouldBeInteractable);
        }
    }
}
