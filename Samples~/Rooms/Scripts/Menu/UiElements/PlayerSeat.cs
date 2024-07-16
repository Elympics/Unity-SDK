using System;
using Elympics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSeat : MonoBehaviour
{
    private const string EmptySeatText = "Waiting for an opponent...";

    [SerializeField] private TextMeshProUGUI playerIdText;
    [SerializeField] private TextMeshProUGUI playerNicknameText;
    [SerializeField] private TextMeshProUGUI playerTeamIndex;
    [SerializeField] private GameObject hostIndicator;
    [SerializeField] private GameObject readinessIndicator;
    [SerializeField] private Button unreadyButton;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image teamMarker;
    [SerializeField] private Color myColor, enemyColor;
    private bool _isMyself = false;
    private Action _onUnready;
    public bool IsOccupied { get; private set; }

    private void OnEnable() => SetEmpty();
    public void InitButton(Action onUnready) => _onUnready = onUnready;
    public void SetPlayer(UserInfo playerInfo, bool isMyself)
    {
        unreadyButton.onClick.AddListener(new UnityEngine.Events.UnityAction(_onUnready));

        Clear();
        SetSeatText(playerInfo);
        IsOccupied = true;
        if (isMyself)
        {
            SetMyselfIndicator();
        }
    }
    public void SetTeamColor(Color color) => teamMarker.color = color;
    public void SetSeatText(UserInfo player)
    {
        playerIdText.text = $"Player Id: {player.UserId}";
        playerNicknameText.text = $"Player Nickname: {player.Nickname}";
        playerTeamIndex.text = $"Player Team Index: {player.TeamIndex}";
        playerIdText.color = new Color(playerIdText.color.r, playerIdText.color.g, playerIdText.color.b, 1);
    }
    public void SetEmpty()
    {
        unreadyButton.onClick.RemoveAllListeners();
        Clear();

        playerIdText.text = EmptySeatText;
        playerNicknameText.text = "";
        playerTeamIndex.text = "";
        playerIdText.color = new Color(playerIdText.color.r, playerIdText.color.g, playerIdText.color.b, 0.5f);

        IsOccupied = false;
        _isMyself = false;
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
        if (_isMyself)
        {
            unreadyButton.gameObject.SetActive(isReady);
            LockUnreadyInteractability(!isReady);
        }
        else
        {
            readinessIndicator.SetActive(isReady);
        }
    }
    public void LockUnreadyInteractability(bool isLocked) => unreadyButton.interactable = !isLocked;
    public void SetHostIndicator() => hostIndicator.SetActive(true);

    private void SetMyselfIndicator()
    {
        _isMyself = true;

        backgroundImage.color = myColor;
    }
}
