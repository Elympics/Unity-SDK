using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
}

public enum RadioButtonStates { Selectable, Selected, NotSelectedDisplay}
