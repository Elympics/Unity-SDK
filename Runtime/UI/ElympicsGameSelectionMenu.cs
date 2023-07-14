using UnityEngine;
using Elympics;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ElympicsGameSelectionMenu : MonoBehaviour
{
    [SerializeField] private ChangeSelectedGameButton gameButtonPrefab = null;

    private List<ChangeSelectedGameButton> buttonsList;

    private ElympicsConfig elympicsConfig;

    private void OnEnable()
    {
        UpdateGameButtonsList();
        UpdateButtonsInteractability();
    }

    private void LoadElympicsConfig()
    {
        elympicsConfig = ElympicsConfig.Load();
        elympicsConfig.CurrentGameSwitched += HandleCurrentGameSwitched;
    }

    [ContextMenu("Update List")]
    public void UpdateGameButtonsList()
    {
        if (elympicsConfig == null)
            LoadElympicsConfig();
        buttonsList ??= new List<ChangeSelectedGameButton>();

        buttonsList.Clear();
        GetComponentsInChildren(buttonsList);

        if (!ValidateGameButtonsList())
        {
            RebuildButtonsList();
            UpdateButtonsInteractability();
        }
    }

    private void RebuildButtonsList()
    {
        foreach (var button in buttonsList)
            DestroyImmediate(button.gameObject);
        buttonsList.Clear();
        var index = 0;
        foreach (var game in elympicsConfig.availableGames)
        {
            var button =
#if UNITY_EDITOR
                PrefabUtility.InstantiatePrefab(gameButtonPrefab, transform) as ChangeSelectedGameButton;
            EditorUtility.SetDirty(button.gameObject);
#else
				Instantiate<ChangeSelectedGameButton>(gameButtonPrefab, transform);
#endif
            button.LinkWithGame(index++, game.GameId, game.GameName, elympicsConfig);
            buttonsList.Add(button);
        }
    }

    private void UpdateButtonsInteractability()
    {
        var currentGameId = elympicsConfig.GetCurrentGameConfig().GameId;
        foreach (var button in buttonsList)
            button.SetInteractable(button.LinkedId != currentGameId);
    }

    private void HandleCurrentGameSwitched() => UpdateButtonsInteractability();

    private bool ValidateGameButtonsList()
    {
        return buttonsList
            .Select(button => button.LinkedId)
            .SequenceEqual(
                elympicsConfig.availableGames
                .Select(game => game.GameId));
    }

    private void OnDestroy()
    {
        if (elympicsConfig != null)
            elympicsConfig.CurrentGameSwitched -= HandleCurrentGameSwitched;
    }
}
