using System;
using Cysharp.Threading.Tasks;
using Elympics;
using Elympics.Models.Authentication;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AuthenticateWindowController : BaseWindow
{
    [SerializeField] private Button startRoomButton;
    [SerializeField] private Button recconectButton;
    [SerializeField] private Button signOutRoomButton;
    [SerializeField] private Button authenticateRoomButton;
    [SerializeField] private Button authenticateWithCacheRoomButton;
    [SerializeField] private Button corruptAuthDataButton;
    [SerializeField] private Button changeRegionButton;

    [SerializeField] private TMP_Dropdown regionsDropdown;
    private RoomController _roomController;

    private const string LoadingText = "Loading...";
    private const string AuthenticateText = "Authenticate";
    private const string StartText = "Start";
    private const string AuthenticateWithCacheText = "Authenticate with cache";
    private const string CachedAuthDataKey = "CachedAuthData";
    private const string ExpiredClientAuthJWT = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIwNTdmMjg4My1iNGI0LTRjYzYtODk1Zi1lMTMzMmRhODY1NjciLCJhdXRoLXR5cGUiOiJjbGllbnQtc2VjcmV0IiwibmJmIjoxNzE4MzczOTc4LCJleHAiOjE3MTg0NjAzNzgsImlhdCI6MTcxODM3Mzk3OH0.O0h2FLCSA69a-D_GLeL6zo_Bqf8D6bW1n8o1Ue8TM1D8bDPv8KPblwBG13JyM76RJf30l7I77RjnYwmYvIxdMn1y8p14QtPkf_-nmxCEyRztE-7el44ud_z7gvzREJ0V0P89_BxPlJfIWG4kXdQGTczERRg4SkQWZyyMtTNNcXtK_KdREmDQm8_QXC9u15xcwVnjUxWfyCevcD-7djl2Sx_S1GFCKJDOsseBtWp8nTAtcCFFEioZDQh0cSf6G773eqFK_sy_jzCNPCGlJ7SCc6qs3MR2Fgg31P3jfQ7vtz1qrVC2mz86WPNQqwXvL9PubfxEL06g5xh9qcGUJuvXAehnaAG6iB098RvvBbHbM55p9cTaXtjk9DalZfMnwAEyEX9dfa6nLQhTMuWjQ8pScGcyG_RybbS932TaTdz_YiVFhnDmGKTugZLWVwLvJPVeri-o8E-BRY4bldKYTX5_ro26jY9tfPgYBi6H8K_alG5hx_A2Hf3Evyd3oWphMl61muReBqmLduL1jUr1V22C4rDPXToQgqhVp_y3p9iGI10tRRmywChFANYeRU2vtBKRQxazvUMCwgjCR8rpHz6JICcP6dlsmgW0WZmc4H0UkC_gAavQVHBpPlq0Ggd8Xf-Ihlx1MymLSCGoid0Ou09vWCAGbiQalnup-TDXjnJINDw";

    private Action _onSignin;
    public void Init(Action onSignin, Action onSignout, Action onStartClicked, RoomController roomController)
    {
        recconectButton.onClick.AddListener(() =>
        {
            _roomController = roomController;
            var joinedGames = ElympicsLobbyClient.Instance.RoomsManager.ListJoinedRooms();
            var joinedMatchId = joinedGames[0].RoomId;
            var isJoined = ElympicsLobbyClient.Instance.RoomsManager.TryGetJoinedRoom(joinedMatchId, out var room);
            _roomController.Recconect(room.RoomId);
            recconectButton.gameObject.SetActive(false);
        });
        _onSignin += onSignin;
        _onSignin += CacheAuthData;

        signOutRoomButton.onClick.AddListener(new UnityEngine.Events.UnityAction(onSignout));
        startRoomButton.onClick.AddListener(new UnityEngine.Events.UnityAction(onStartClicked));

        if (ElympicsLobbyClient.Instance.IsAuthenticated && ElympicsLobbyClient.Instance.WebSocketSession.IsConnected)
        {
            ElympicsLobbyClient.Instance.RoomsManager.RoomListUpdated += OnRoomListUpdated;
            _onSignin?.Invoke();
        }
        else
            EnableAuthenticateView();
    }
    private void SetAuthenticateViewActive(bool active)
    {
        authenticateRoomButton.gameObject.SetActive(active);
        authenticateWithCacheRoomButton.gameObject.SetActive(active);
        corruptAuthDataButton.gameObject.SetActive(active);
    }
    private void SetAuthenticateViewInteractable(bool interactable)
    {
        authenticateRoomButton.interactable = interactable;
        authenticateWithCacheRoomButton.interactable = interactable;
        corruptAuthDataButton.interactable = interactable;
    }
    private void SetStartViewActive(bool active)
    {
        signOutRoomButton.gameObject.SetActive(active);
        changeRegionButton.gameObject.SetActive(active);
        startRoomButton.gameObject.SetActive(active);

        var inGame = ElympicsLobbyClient.Instance.RoomsManager.ListJoinedRooms().Count != 0;
        recconectButton.gameObject.SetActive(active && inGame);
    }
    private void SetStartViewInteractable(bool interactable)
    {
        signOutRoomButton.interactable = interactable;
        changeRegionButton.interactable = interactable;
        startRoomButton.interactable = interactable;
        recconectButton.interactable = interactable;
    }
    public void Deinit()
    {
        _onSignin = null;

        signOutRoomButton.onClick.RemoveAllListeners();
        startRoomButton.onClick.RemoveAllListeners();
    }
    [UsedImplicitly]
    public void AuthenticateRoomWithCache() => AuthenticateRoomAsync(true).Forget();
    [UsedImplicitly]
    public void AuthenticateRoom() => AuthenticateRoomAsync(false).Forget();
    private async UniTaskVoid AuthenticateRoomAsync(bool logWithCachedData)
    {
        try
        {
            ElympicsLobbyClient.Instance.RoomsManager.RoomListUpdated += OnRoomListUpdated;

            OnAuthenticationBegin();
            await ElympicsLobbyClient.Instance.ConnectToElympicsAsync(new ConnectionData()
            {
                AuthType = AuthType.ClientSecret,
                Region = new RegionData()
                {
                    Name = regionsDropdown.options[regionsDropdown.value].text,
                },
                AuthFromCacheData = logWithCachedData ? LoadCachedData() : null
            });
            _onSignin?.Invoke();
        }
        catch (Exception ex)
        {
            ElympicsLobbyClient.Instance.RoomsManager.RoomListUpdated -= OnRoomListUpdated;

            Debug.LogWarning($"Authentication failed: {ex.Message}");

        }
        finally
        {
            OnAuthenticationEnd();
            if (!ElympicsLobbyClient.Instance.IsAuthenticated && logWithCachedData)
            {
                Debug.LogWarning($"Try to log with new auth data");
                AuthenticateRoomAsync(false).Forget();
            }
        }
    }
    public void OnAuthenticationBegin()
    {
        SetStartViewActive(false);
        SetAuthenticateViewActive(true);
        SetAuthenticateViewInteractable(false);
    }
    public void OnAuthenticationEnd()
    {
        if (ElympicsLobbyClient.Instance.IsAuthenticated)
            SetAuthenticateViewActive(false);
        else
        {
            SetStartViewActive(false);
            SetAuthenticateViewActive(true);
        }
        SetAuthenticateViewInteractable(true);
    }
    [UsedImplicitly]
    public void ChangeRegion() => TryChangeRegion().Forget();
    public async UniTaskVoid TryChangeRegion()
    {
        try
        {
            ElympicsLobbyClient.Instance.RoomsManager.RoomListUpdated += OnRoomListUpdated;
            var region = new RegionData()
            {
                Name = regionsDropdown.options[regionsDropdown.value].text,
            };
            var connectionData = new ConnectionData()
            {
                AuthFromCacheData = null,
                AuthType = null,
                Region = region
            };

            OnAuthenticationBegin();
            await ElympicsLobbyClient.Instance.ConnectToElympicsAsync(connectionData);
            _onSignin?.Invoke();
        }
        catch (Exception ex)
        {
            ElympicsLobbyClient.Instance.RoomsManager.RoomListUpdated -= OnRoomListUpdated;
            Debug.LogError($"Authentication failed: {ex.Message}");
        }
        finally
        {
            OnAuthenticationEnd();
        }
    }
    [UsedImplicitly]
    public void CoruptCacheSaveData()
    {
        var currentAuthData = LoadCachedData().CachedData;

        var userId = currentAuthData.UserId.ToString();
        var nickName = currentAuthData.Nickname;
        var authType = currentAuthData.AuthType;

        var coruptedAuthData = new AuthDataSave(userId, ExpiredClientAuthJWT, nickName, authType);
        var jsonData = JsonUtility.ToJson(coruptedAuthData);

        PlayerPrefs.SetString(CachedAuthDataKey, jsonData);
    }
    public void CacheAuthData()
    {
        var currentAuthData = ElympicsLobbyClient.Instance.AuthData;
        if (currentAuthData == null)
            return;
        var userId = currentAuthData.UserId.ToString();
        var JwtToken = currentAuthData.JwtToken;
        var nickName = currentAuthData.Nickname;
        var authType = currentAuthData.AuthType;

        var authData = new AuthDataSave(userId, JwtToken, nickName, authType);
        var jsonData = JsonUtility.ToJson(authData);

        PlayerPrefs.SetString(CachedAuthDataKey, jsonData);
    }
    private CachedAuthData LoadCachedData()
    {
        if (!PlayerPrefs.HasKey(CachedAuthDataKey))
            return new CachedAuthData();

        var jsonData = PlayerPrefs.GetString(CachedAuthDataKey);
        var authSave = JsonUtility.FromJson<AuthDataSave>(jsonData);

        return new CachedAuthData()
        {
            CachedData = new AuthData(Guid.Parse(authSave.id), authSave.jwToken, authSave.nickname, authSave.type)
        };
    }
    private void OnRoomListUpdated(RoomListUpdatedArgs _)
    {
        ElympicsLobbyClient.Instance.RoomsManager.RoomListUpdated -= OnRoomListUpdated;
        EnableStartView();
    }
    private void EnableStartView()
    {
        SetStartViewActive(true);
        SetStartViewInteractable(true);
        SetAuthenticateViewActive(false);
        SetAuthenticateViewInteractable(false);
    }
    public void EnableAuthenticateView()
    {
        SetStartViewActive(false);
        SetStartViewInteractable(false);
        SetAuthenticateViewActive(true);
        SetAuthenticateViewInteractable(true);
    }
    public override void Show()
    {
        base.Show();
        if (ElympicsLobbyClient.Instance.IsAuthenticated && ElympicsLobbyClient.Instance.WebSocketSession.IsConnected)
            EnableStartView();
        else
            EnableAuthenticateView();
    }
}

