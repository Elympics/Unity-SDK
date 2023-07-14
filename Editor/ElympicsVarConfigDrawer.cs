using System.Reflection;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEditor;
using UnityEngine;

namespace Elympics
{
    [CustomPropertyDrawer(typeof(ConfigForVarAttribute))]
    public class ElympicsVarConfigDrawer : PropertyDrawer
    {
        private bool _initialized;
        private bool _initializedPlayMode;

        private string _varName;
        private SerializedProperty _enabledProperty;
        private SerializedProperty _toleranceProperty;

        private ElympicsVar _elympicsVar;
        private ElympicsVarEqualityComparer _comparer;

        private const uint NumOfDisplayedProperties = 2U;
        private const string EnabledTooltipForClone = "Synchronized contents selection should be performed only in the main window!";
        private const string MultiObjectTooltip = "Multi-object editing is not supported.";

        private bool Initialize(SerializedProperty property)
        {
            var configForVarAttribute = attribute as ConfigForVarAttribute;
            _varName = configForVarAttribute.DisplayName ?? configForVarAttribute.ElympicsVarPropertyName;
            _enabledProperty = property.FindPropertyRelative(nameof(ElympicsVarConfig.synchronizationEnabled));
            _toleranceProperty = property.FindPropertyRelative(nameof(ElympicsVarConfig.tolerance));

            EditorApplication.playModeStateChanged += change =>
            {
                if (change == PlayModeStateChange.ExitingPlayMode)
                    _initializedPlayMode = false;
            };

            _initialized = true;
            return _initialized;
        }

        private bool InitializePlayMode(SerializedProperty property)
        {
            var configForVarAttribute = attribute as ConfigForVarAttribute;
            var targetObject = property.serializedObject.targetObject;
            _elympicsVar = targetObject.GetType().GetField(configForVarAttribute.ElympicsVarPropertyName, BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(targetObject) as ElympicsVar;
            _comparer = _elympicsVar?.GetType().GetProperty("Comparer")?.GetValue(_elympicsVar) as ElympicsVarEqualityComparer;

            if (_comparer != null)
                _initializedPlayMode = true;

            return _initializedPlayMode;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!_initialized)
                if (!Initialize(property))
                    return;

            if (EditorApplication.isPlaying && !_initializedPlayMode)
                if (!InitializePlayMode(property))
                    return;

            var currentFieldIndex = 0U;
            EditorGUI.BeginDisabledGroup(property.serializedObject.isEditingMultipleObjects);
            var generalLabel = new GUIContent((string)null, property.serializedObject.isEditingMultipleObjects ? MultiObjectTooltip : null);

            var enabledLabel = new GUIContent(generalLabel) { text = $"Synchronize {_varName}" };
            if (enabledLabel.tooltip == null && ElympicsClonesManager.IsClone())
                enabledLabel.tooltip = EnabledTooltipForClone;
            var currentRect = GetSingleLinePropertyRect(position.position, position.width, currentFieldIndex++);
            _ = EditorGUI.BeginProperty(currentRect, enabledLabel, _enabledProperty);
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying || ElympicsClonesManager.IsClone());
            var currentEnabledValue = EditorApplication.isPlaying ? _elympicsVar.EnabledSynchronization : _enabledProperty.boolValue;
            EditorGUI.BeginChangeCheck();
            var newEnabledValue = EditorGUI.ToggleLeft(currentRect, enabledLabel, currentEnabledValue);
            if (EditorGUI.EndChangeCheck())
                _enabledProperty.boolValue = newEnabledValue;
            EditorGUI.EndDisabledGroup();
            EditorGUI.EndProperty();

            var toleranceLabel = new GUIContent(generalLabel) { text = _toleranceProperty.displayName };
            currentRect = GetSingleLinePropertyRect(position.position, position.width, currentFieldIndex++);
            _ = EditorGUI.BeginProperty(currentRect, null, _toleranceProperty);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUI.BeginDisabledGroup(!_enabledProperty.boolValue);
                var currentToleranceValue = EditorApplication.isPlaying ? _comparer.Tolerance : _toleranceProperty.floatValue;
                EditorGUI.BeginChangeCheck();
                var newToleranceValue = Mathf.Max(EditorGUI.FloatField(currentRect, toleranceLabel, currentToleranceValue), 0f);
                if (EditorGUI.EndChangeCheck())
                {
                    _toleranceProperty.floatValue = newToleranceValue;
                    if (EditorApplication.isPlaying)
                        _comparer.Tolerance = newToleranceValue;
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUI.EndProperty();

            EditorGUI.EndDisabledGroup();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            NumOfDisplayedProperties * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

        private Rect GetSingleLinePropertyRect(Vector2 position, float width, uint index = 0)
        {
            var singleLinePropertyRect = new Rect(position.x, position.y, width, EditorGUIUtility.singleLineHeight);
            singleLinePropertyRect.y += index * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            return singleLinePropertyRect;
        }
    }
}
