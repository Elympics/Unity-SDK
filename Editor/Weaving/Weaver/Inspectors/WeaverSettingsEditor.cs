using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Elympics.Weaver.Editors
{
    [CustomEditor(typeof(WeaverSettings))]
    public class WeaverSettingsEditor : UnityEditor.Editor
    {
        public class Styles
        {
            public GUIStyle zebraStyle;
            public GUIContent cachedContent;

            public Styles()
            {
                cachedContent = new GUIContent();

                var altTexutre = new Texture2D(1, 1);
                altTexutre.SetPixel(0, 0, new Color32(126, 126, 126, 50));
                altTexutre.Apply();

                var selectedTexture = new Texture2D(1, 1);
                selectedTexture.SetPixel(0, 0, new Color32(0, 140, 255, 40));
                selectedTexture.Apply();

                zebraStyle = new GUIStyle(GUI.skin.label)
                {
                    onHover = { background = altTexutre },
                    onFocused = { background = selectedTexture }
                };
                // Set Color
                var zebraFontColor = zebraStyle.normal.textColor;
                zebraStyle.onFocused.textColor = zebraFontColor;
                zebraStyle.onHover.textColor = zebraFontColor;

                // Set Height
                zebraStyle.fixedHeight = 20;
                zebraStyle.alignment = TextAnchor.MiddleLeft;

                zebraStyle.richText = true;
            }

            public GUIContent Content(string message)
            {
                cachedContent.text = message;
                return cachedContent;
            }
        }

        // Properties
        private SerializedProperty _weavedAssemblies;
        private SerializedProperty _enabled;
        private SerializedProperty _isSymbolsDefined;
        private SerializedProperty _requiredScriptingSymbols;
        private Log _log;

        // Lists
        private ReorderableList _weavedAssembliesList;

        // Layouts
        private Vector2 _logScrollPosition;
        private int _selectedLogIndex;

        // Labels
        private GUIContent _weavedAssemblyHeaderLabel;
        private static Styles styles;

        private bool _hasModifiedProperties;

        private void OnEnable()
        {
            AssemblyUtility.PopulateAssemblyCache();
            _weavedAssemblies = serializedObject.FindProperty("m_WeavedAssemblies");
            _enabled = serializedObject.FindProperty("m_IsEnabled");

            // Get the log
            _log = serializedObject.FindField<Log>("m_Log").value;

            _requiredScriptingSymbols = serializedObject.FindProperty("m_RequiredScriptingSymbols");
            _isSymbolsDefined = _requiredScriptingSymbols.FindPropertyRelative("m_IsActive");
            _weavedAssembliesList = new ReorderableList(serializedObject, _weavedAssemblies);
            _weavedAssembliesList.drawHeaderCallback += OnWeavedAssemblyDrawHeader;
            _weavedAssembliesList.drawElementCallback += OnWeavedAssemblyDrawElement;
            _weavedAssembliesList.onAddCallback += OnWeavedAssemblyElementAdded;
            _weavedAssembliesList.drawHeaderCallback += OnWeavedAssemblyHeader;
            _weavedAssembliesList.onRemoveCallback += OnWeavedAssemblyRemoved;
            // Labels
            _weavedAssemblyHeaderLabel = new GUIContent("Weaved Assemblies");
        }

        private void OnDisable()
        {
            if (_hasModifiedProperties)
            {
                var title = "Weaver Settings Pending Changes";
                var message = "You currently have some pending changes that have not been applied and will be lost. Would you like to apply them now?";
                var ok = "Apply Changes";
                var cancel = "Discard Changes";
                var shouldApply = EditorUtility.DisplayDialog(title, message, ok, cancel);
                if (shouldApply)
                {
                    ApplyModifiedProperties();
                }
                _hasModifiedProperties = false;
            }
        }

        private void OnWeavedAssemblyDrawHeader(Rect rect)
        {
            GUI.Label(rect, WeaverContent.SettingsWeavedAsesmbliesTitle);
        }

        private void OnWeavedAssemblyRemoved(ReorderableList list)
        {
            _weavedAssemblies.DeleteArrayElementAtIndex(list.index);
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            {
                styles ??= new Styles();

                GUILayout.Label("Settings", EditorStyles.boldLabel);

                _ = EditorGUILayout.PropertyField(_enabled);
                _ = EditorGUILayout.PropertyField(_requiredScriptingSymbols);

                if (!_enabled.boolValue)
                {
                    EditorGUILayout.HelpBox("Weaver will not run as it's currently disabled.", MessageType.Info);
                }
                else if (!_isSymbolsDefined.boolValue)
                {
                    EditorGUILayout.HelpBox("Weaver will not run the required scripting symbols are not defined.", MessageType.Info);
                }
                GUILayout.Box(GUIContent.none, GUILayout.Height(3f), GUILayout.ExpandWidth(true));

                _weavedAssembliesList.DoLayoutList();
            }
            if (EditorGUI.EndChangeCheck())
            {
                _hasModifiedProperties = true;
            }
            if (_hasModifiedProperties)
            {
                if (GUILayout.Button("Apply Modified Properties"))
                {
                    ApplyModifiedProperties();
                }
            }
            GUILayout.Label("Log", EditorStyles.boldLabel);
            DrawLogs();


        }

        private void ApplyModifiedProperties()
        {
            _hasModifiedProperties = false;
            _ = serializedObject.ApplyModifiedProperties();
            AssemblyUtility.DirtyAllScripts();
            serializedObject.Update();
        }

        private void DrawLogs()
        {
            _logScrollPosition = EditorGUILayout.BeginScrollView(_logScrollPosition, EditorStyles.textArea);
            {
                for (var i = 0; i < _log.entries.Count; i++)
                {
                    var entry = _log.entries[i];
                    styles ??= new Styles();

                    var position = GUILayoutUtility.GetRect(styles.Content(entry.message), styles.zebraStyle);
                    // Input
                    var controlID = GUIUtility.GetControlID(321324, FocusType.Keyboard, position);
                    var current = Event.current;
                    var eventType = current.GetTypeForControl(controlID);
                    if (eventType == EventType.MouseDown && position.Contains(current.mousePosition))
                    {
                        GUIUtility.keyboardControl = controlID;
                        _selectedLogIndex = i;
                        current.Use();
                        GUI.changed = true;
                    }

                    if (current.type == EventType.KeyDown)
                    {
                        if (current.keyCode == KeyCode.UpArrow && _selectedLogIndex > 0)
                        {
                            _selectedLogIndex--;
                            current.Use();
                        }

                        if (current.keyCode == KeyCode.DownArrow && _selectedLogIndex < _log.entries.Count - 1)
                        {
                            _selectedLogIndex++;
                            current.Use();
                        }
                    }


                    if (eventType == EventType.Repaint)
                    {
                        var isHover = entry.id % 2 == 0;
                        var isActive = false;
                        var isOn = true;
                        var hasKeyboardFocus = _selectedLogIndex == i;
                        styles.zebraStyle.Draw(position, styles.Content(entry.message), isHover, isActive, isOn, hasKeyboardFocus);
                    }
                }

                if (_selectedLogIndex < 0 || _selectedLogIndex >= _log.entries.Count)
                {
                    // If we go out of bounds we zero out our selection
                    _selectedLogIndex = -1;
                }
            }
            EditorGUILayout.EndScrollView();
        }

        #region -= Weaved Assemblies =-
        private void OnWeavedAssemblyDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var indexProperty = _weavedAssemblies.GetArrayElementAtIndex(index);
            _ = EditorGUI.PropertyField(rect, indexProperty);
        }

        private void OnWeavedAssemblyElementAdded(ReorderableList list)
        {
            var menu = new GenericMenu();

            var cachedAssemblies = AssemblyUtility.GetUserCachedAssemblies();

            for (var x = 0; x < cachedAssemblies.Count; x++)
            {
                var foundMatch = false;
                for (var y = 0; y < _weavedAssemblies.arraySize; y++)
                {
                    var current = _weavedAssemblies.GetArrayElementAtIndex(y);
                    var assetPath = current.FindPropertyRelative("m_RelativePath");
                    if (cachedAssemblies[x].Location.IndexOf(assetPath.stringValue, StringComparison.Ordinal) > 0)
                    {
                        foundMatch = true;
                        break;
                    }
                }
                if (!foundMatch)
                {
                    var content = new GUIContent(cachedAssemblies[x].Name);
                    var projectPath = FileUtility.SystemToProjectPath(FileUtility.Normalize(cachedAssemblies[x].Location));
                    menu.AddItem(content, false, OnWeavedAssemblyAdded, projectPath);
                }
            }

            if (menu.GetItemCount() == 0)
            {
                menu.AddDisabledItem(new GUIContent("[All Assemblies Added]"));
            }

            menu.ShowAsContext();
        }

        private void OnWeavedAssemblyHeader(Rect rect)
        {
            GUI.Label(rect, _weavedAssemblyHeaderLabel);
        }

        private void OnWeavedAssemblyAdded(object path)
        {
            _weavedAssemblies.arraySize++;
            var weaved = _weavedAssemblies.GetArrayElementAtIndex(_weavedAssemblies.arraySize - 1);
            weaved.FindPropertyRelative("m_RelativePath").stringValue = (string)path;
            weaved.FindPropertyRelative("m_IsActive").boolValue = true;
        }
        #endregion
    }
}
