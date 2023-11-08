using UnityEngine;

public class RadioButtonGroup : MonoBehaviour
{
    [SerializeField] private int defaultOptionIndex;
    private RadioButtonOption[] options;
    private int currentOptionIndex;

    public int CurrentOptionIndex => currentOptionIndex;

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
        for (int i = 0; i < options.Length; i++)
        {
            bool isChosenOption = options[i] == chosenOption;
            if (isChosenOption) currentOptionIndex = i;
            options[i].SetOptionState(isChosenOption ? RadioButtonStates.Selected : RadioButtonStates.Selectable);
        }
    }

    public void SetToDisplay(int selectedOption)
    {
        for (int i = 0; i < options.Length; i++)
        {
            if (selectedOption == i) continue;
            options[i].SetOptionState(RadioButtonStates.NotSelectedDisplay);
        }
    }
}
