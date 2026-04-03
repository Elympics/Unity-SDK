using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

public class RoomsNavigationController : MonoBehaviour
{
    [SerializeField] private AuthenticateWindowController authenticateWindowController;
    [SerializeField] private RoomChoiceController roomChoiceController;
    [SerializeField] private RoomCreationController roomCreationController;
    [SerializeField] private RoomController roomController;
    [Space]
    [SerializeField] private BaseWindow currentViewController;

    public static RoomsNavigationController Instance;
    public void Init()
    {
        Assert.IsNotNull(authenticateWindowController);
        Assert.IsNotNull(roomChoiceController);
        Assert.IsNotNull(roomCreationController);
        Assert.IsNotNull(roomController);
        Assert.IsNotNull(currentViewController);

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Two RoomsNavigationControllers in the scene. Destroying one.");
            Destroy(gameObject);
        }
    }
    public void Deinit() => Instance = null;

    [UsedImplicitly]
    public void ShowRoomChoiceView() => ShowView(roomChoiceController);

    public void ShowTitleScreen() => ShowView(authenticateWindowController);

    public void ShowRoomCreationView() => ShowView(roomCreationController);

    public void ShowRoomView() => ShowView(roomController);

    private void ShowView(BaseWindow newView)
    {
        currentViewController.Hide();
        currentViewController = newView;
        currentViewController.Show();
    }
}
