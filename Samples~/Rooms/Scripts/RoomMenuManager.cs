using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject titleScreen;
    [SerializeField] private GameObject roomChoice;
    [SerializeField] private GameObject roomCreation;
    [SerializeField] private GameObject roomView;

    private GameObject currentWindow;

    private void Awake()
    {
        currentWindow = roomChoice;
        roomChoice.SetActive(true);
        roomCreation.SetActive(false);
        roomView.SetActive(false);
    }

    public void TurnOnRoomChoiceWindow()
    {
        TurnOffCurrentWindow();
        currentWindow = roomChoice;
        currentWindow.SetActive(true);
        //TODO SetUpNewWindow
    }

    public void TurnOnRoomCreationWindow()
    {
        TurnOffCurrentWindow();
        currentWindow = roomCreation;
        currentWindow.SetActive(true);
    }

    public void TurnOnRoomViewWindow()
    {
        TurnOffCurrentWindow();
        currentWindow = roomView;
        currentWindow.SetActive(true);
        //TODO SetUpNewWindow
    }

    private void TurnOffCurrentWindow()
    {
        currentWindow.SetActive(false);
    }

}
