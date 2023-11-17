using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class StateDependentButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Image buttonImage;
    [Space]
    [SerializeField] private int currentState = 0;
    [SerializeField] private List<ButtonState> buttonStates = new List<ButtonState>();

    public void Click() => buttonStates[currentState].OnClickEvent?.Invoke();

    public void SetState(int stateIndex)
    {
        currentState = stateIndex;
        buttonText.text = buttonStates[currentState].stateText;
        if (buttonStates[currentState].stateImage != null)
        {
            buttonImage.sprite = buttonStates[currentState].stateImage;
        }
        button.interactable = buttonStates[currentState].interactable;
    }

    public void ManageInteractability(bool shouldBeInteractable)
    {
        button.interactable = shouldBeInteractable;
    }

    private void OnValidate()
    {
        try
        {
            SetState(currentState);
        }
        catch { }
    }

#nullable enable
    [Serializable]
    private struct ButtonState
    {
        public string stateText;
        public Sprite stateImage;
        public bool interactable;
        public UnityEvent OnClickEvent;
    }

}
