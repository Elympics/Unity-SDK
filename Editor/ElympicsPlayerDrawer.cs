using System;
using UnityEditor;
using UnityEngine;

namespace Elympics
{
    [CustomPropertyDrawer(typeof(ElympicsPlayer))]
    public class ElympicsPlayerDrawer : PropertyDrawer
    {
        private const string AllOption = "All";
        private const string NoneOption = "None";
        private const string PlayerOption = "Player";
        private const string InvalidOption = "Invalid";

        private static readonly string[] Options = { AllOption, NoneOption, PlayerOption };
        private static readonly string[] OptionsWithInvalid = { AllOption, NoneOption, PlayerOption, InvalidOption };

        private readonly int _allOptionIndex = Array.IndexOf(Options, AllOption);
        private readonly int _noneOptionIndex = Array.IndexOf(Options, NoneOption);
        private readonly int _playerOptionIndex = Array.IndexOf(Options, PlayerOption);
        private readonly int _invalidOptionIndex = Array.IndexOf(OptionsWithInvalid, InvalidOption);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var playerIndexProperty = property.FindPropertyRelative(nameof(ElympicsPlayer.playerIndex));
            var player = new ElympicsPlayer { playerIndex = playerIndexProperty.intValue };

            int previousChosenOption;
            if (player == ElympicsPlayer.All)
                previousChosenOption = _allOptionIndex;
            else if (player == ElympicsPlayer.World)
                previousChosenOption = _noneOptionIndex;
            else if (player == ElympicsPlayer.Invalid)
                previousChosenOption = _invalidOptionIndex;
            else
                previousChosenOption = _playerOptionIndex;

            position.width /= 2;
            var newChosenOption = EditorGUI.Popup(position, label.text, previousChosenOption, OptionsWithInvalid);
            position.x += position.width;

            if (newChosenOption == _allOptionIndex)
                DrawDisabledPlayerTextField(position, playerIndexProperty, ElympicsPlayer.All);
            else if (newChosenOption == _noneOptionIndex)
                DrawDisabledPlayerTextField(position, playerIndexProperty, ElympicsPlayer.World);
            else if (newChosenOption == _invalidOptionIndex)
                DrawDisabledPlayerTextField(position, playerIndexProperty, ElympicsPlayer.Invalid);
            else
                DrawPlayerTextField(position, previousChosenOption, player, playerIndexProperty);
        }

        private static void DrawDisabledPlayerTextField(Rect position, SerializedProperty playerIndexProperty, ElympicsPlayer player)
        {
            EditorGUI.BeginDisabledGroup(true);
            _ = EditorGUI.TextField(position, player.playerIndex.ToString());
            playerIndexProperty.intValue = player.playerIndex;
            EditorGUI.EndDisabledGroup();
        }

        private void DrawPlayerTextField(Rect position, int previousChosenOption, ElympicsPlayer player, SerializedProperty playerIndexProperty)
        {
            int playerIndex;
            if (previousChosenOption != _playerOptionIndex)
                playerIndex = 0;
            else
                playerIndex = (int)player;

            var newPlayerIndexStr = EditorGUI.TextField(position, playerIndex.ToString());
            if (int.TryParse(newPlayerIndexStr, out var newPlayerIndex))
            {
                var newPlayer = ElympicsPlayer.FromIndex(newPlayerIndex);
                playerIndexProperty.intValue = newPlayer.playerIndex;
            }
        }
    }
}
