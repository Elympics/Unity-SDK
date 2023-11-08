using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSeat : MonoBehaviour
{
    private const string EmptySeatText = "Waiting for an opponent...";

    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private GameObject hostIndicator;
    [SerializeField] private GameObject readinessIndicator;
    [SerializeField] private Button unreadyButton;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color myColor, enemyColor;

    private bool isMyself = false;

    public bool IsOccupied { get; private set; }

    private void OnEnable()
    {
        SetEmpty();
    }

    public void SetPlayer(Guid playerId, bool isMyself)
    {
        Clear();

        playerNameText.text = playerId.ToString();
        playerNameText.color = new Color(playerNameText.color.r, playerNameText.color.g, playerNameText.color.b, 1);

        IsOccupied = true;

        if (isMyself)
        {
            SetMyselfIndicator();
        }
    }

    public void SetEmpty()
    {
        Clear();

        playerNameText.text = EmptySeatText;
        playerNameText.color = new Color(playerNameText.color.r, playerNameText.color.g, playerNameText.color.b, 0.5f);

        IsOccupied = false;
        isMyself = false;
    }

    private void Clear()
    {
        hostIndicator.SetActive(false);
        backgroundImage.color = enemyColor;
        SetReady(false);
        unreadyButton.gameObject.SetActive(false);
    }

    public void SetReady(bool isReady)
    {
        readinessIndicator.SetActive(isReady);

        if (isMyself)
            LockUnreadyInteractability(!isReady);
    }

    public void LockUnreadyInteractability(bool isLocked) => unreadyButton.interactable = !isLocked;
    public void SetHostIndicator() => hostIndicator.SetActive(true);

    private void SetMyselfIndicator()
    {
        isMyself = true;

        backgroundImage.color = myColor;
        unreadyButton.gameObject.SetActive(true);
    }
}
