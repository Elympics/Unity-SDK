using TMPro;
using UnityEngine;

public class RoomsListNumberDisplayer : MonoBehaviour
{
    [SerializeField] private string formattableText = "<sprite name=\"room\">Room({0})";
    [SerializeField] private TextMeshProUGUI roomsNumberLabel;
    [SerializeField] private RoomChoiceController roomChoiceController;

    private void Awake()
    {
        SetNumber(0);

        roomChoiceController.ListLengthChanged += SetNumber;
    }

    private void OnDestroy()
    {
        roomChoiceController.ListLengthChanged -= SetNumber;
    }

    private void SetNumber(int number) => roomsNumberLabel.text = string.Format(formattableText, number);
}
