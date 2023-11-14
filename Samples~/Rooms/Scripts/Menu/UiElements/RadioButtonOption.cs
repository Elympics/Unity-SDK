using UnityEngine;

[RequireComponent(typeof(StateDependentButton))]
public class RadioButtonOption : MonoBehaviour
{
    [SerializeField] private GameObject selectedIcon;
    private StateDependentButton button;
    private RadioButtonGroup radioButtonManager;

    public void SetUpOption(RadioButtonGroup manager)
    {
        radioButtonManager = manager;
        button = GetComponent<StateDependentButton>();
    }

    public void OptionClicked() => radioButtonManager.SelectOption(this);

    public void SetOptionState(RadioButtonStates newState)
    {
        selectedIcon.SetActive(newState != RadioButtonStates.Selectable);
        button.SetState((int)newState);
    }

    public void ManageInteractability(bool shouldBeInteractable)
    {
        button.ManageInteractability(shouldBeInteractable);
    }
}

public enum RadioButtonStates { Selectable, Selected, NotSelectedDisplay }
