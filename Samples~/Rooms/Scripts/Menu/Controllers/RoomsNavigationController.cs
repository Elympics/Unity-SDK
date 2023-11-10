using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

public class RoomsNavigationController : MonoBehaviour
{
    [SerializeField] private BaseWindow titleScreenController;
    [SerializeField] private RoomChoiceController roomChoiceController;
    [SerializeField] private RoomCreationController roomCreationController;
    [SerializeField] private RoomController roomController;

    [SerializeField] private BaseWindow currentViewController;

    public static RoomsNavigationController Instance;

    private void Awake()
    {
        Assert.IsNotNull(titleScreenController);
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

    private void OnDestroy()
    {
        Instance = null;
    }

    [UsedImplicitly]
    public void ShowRoomChoiceView() => ShowView(roomChoiceController);

    public void ShowTitleScreen() => ShowView(titleScreenController);

    public void ShowRoomCreationView() => ShowView(roomCreationController);

    public void ShowRoomView() => ShowView(roomController);

    private void ShowView(BaseWindow newView)
    {
        currentViewController.Hide();
        currentViewController = newView;
        currentViewController.Show();
    }
}
