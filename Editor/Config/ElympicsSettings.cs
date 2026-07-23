using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable

namespace Elympics.Editor.Config
{
    internal class ElympicsSettings : ScriptableObject
    {
        public VisualTreeAsset? settingsUxml;

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/Elympics", SettingsScope.Project)
            {
                activateHandler = (_, root) =>
                {
                    var instance = CreateInstance<ElympicsSettings>();
                    if (instance.settingsUxml != null)
                        root.Add(PrepareInspectorTree(instance.settingsUxml));
                },
                keywords = new HashSet<string>(new[] { "Elympics", "Define" })
            };
        }

        private static VisualElement PrepareInspectorTree(VisualTreeAsset sourceTree)
        {
            VisualElement settingsTree = sourceTree.CloneTree();

            var scriptingDefinesContainer = settingsTree.Q<VisualElement>("scripting-defines-container");
            foreach (var defineListPath in AssetDatabase.FindAssets($"t:{nameof(ElympicsDefineList)}"))
                foreach (var define in AssetDatabase.LoadAssetAtPath<ElympicsDefineList>(AssetDatabase.GUIDToAssetPath(defineListPath)))
                    scriptingDefinesContainer.Add(new Toggle(define.Name) { tooltip = define.Description });
            foreach (var child in scriptingDefinesContainer.Children())
            {
                if (child is not Toggle toggle)
                    continue;
                var name = toggle.label;
                PlayerSettings.GetScriptingDefineSymbols(GetActiveNamedBuildTarget(), out var initialDefines);
                toggle.value = initialDefines.Contains(name);
                _ = toggle.RegisterValueChangedCallback(_ =>
                {
                    PlayerSettings.GetScriptingDefineSymbols(GetActiveNamedBuildTarget(), out var currentDefines);
                    PlayerSettings.SetScriptingDefineSymbols(GetActiveNamedBuildTarget(), toggle.value
                        ? currentDefines.Append(name).ToArray()
                        : currentDefines.Where(s => s != name).ToArray());
                });
            }

            return settingsTree;

            static NamedBuildTarget GetActiveNamedBuildTarget()
            {
                var target = EditorUserBuildSettings.activeBuildTarget;
                var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
                return buildTargetGroup == BuildTargetGroup.Standalone && EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server
                    ? NamedBuildTarget.Server
                    : NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            }
        }
    }
}
