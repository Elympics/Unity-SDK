using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class RadioButtonOption : MonoBehaviour
{
    [SerializeField] private GameObject selectedIcon;

    private RadioButtonGroup radioButtonManager;
    private Button button;
    private ColorBlock activeColors, disabledColors;

    public void Init(RadioButtonGroup manager)
    {
        radioButtonManager = manager;
        button = GetComponent<Button>();

        disabledColors = button.colors;
        activeColors = button.colors;
        activeColors.disabledColor = button.colors.normalColor;
        button.colors = activeColors;

        ReactToSelection(false);
    }

    [UsedImplicitly]
    public void OptionClicked() => radioButtonManager.SelectOption(this);

    public void ReactToSelection(bool selected, bool isParentInteractable = true)
    {
        selectedIcon.SetActive(selected);
        button.interactable = !selected && isParentInteractable;
    }

    public void ManageInteractabilty(bool shouldBeInteractable)
    {
        button.interactable = shouldBeInteractable;

        button.colors = shouldBeInteractable ? activeColors : disabledColors;
    }
}
