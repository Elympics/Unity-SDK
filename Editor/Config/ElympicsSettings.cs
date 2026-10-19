using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elympics.Core.Logger;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable

namespace Elympics.Editor.Config
{
    internal class ElympicsSettings : ScriptableObject
    {
        private const string SdkPackagePath = "Packages/com.daftmobile.elympics/";

        public VisualTreeAsset? settingsUxml;

        /// <summary>Every build target Unity knows, whether or not its build support is installed.</summary>
        private static readonly NamedBuildTarget[] AllNamedBuildTargets = typeof(NamedBuildTarget)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(NamedBuildTarget) && !field.IsDefined(typeof(ObsoleteAttribute)))
            .Select(field => (NamedBuildTarget)field.GetValue(null))
            .Where(target => target != NamedBuildTarget.Unknown)
            .ToArray();

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
            // Lists come from every package (the SDK's first) and from Assets.
            var defineListPaths = AssetDatabase.FindAssets($"t:{nameof(ElympicsDefineList)}", new[] { "Assets", "Packages" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .OrderBy(path => !path.StartsWith(SdkPackagePath))
                .ThenBy(path => path);
            foreach (var defineListPath in defineListPaths)
                foreach (var define in AssetDatabase.LoadAssetAtPath<ElympicsDefineList>(defineListPath))
                    scriptingDefinesContainer.Add(CreateDefineRow(define));

            return settingsTree;

            static VisualElement CreateDefineRow(ElympicsDefineList.ElympicsDefineDescription define)
            {
                var name = define.Name;
                var usesAllTargets = define.BuildTargets.Count == 0;
                var targets = usesAllTargets
                    ? AllNamedBuildTargets
                    : define.BuildTargets.Select(ParseNamedBuildTarget).OfType<NamedBuildTarget>().ToArray();

                var toggle = new Toggle(name);
                var definedCount = targets.Count(target =>
                {
                    PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
                    return defines.Contains(name);
                });
                toggle.SetValueWithoutNotify(targets.Length > 0 && definedCount == targets.Length);
                toggle.showMixedValue = definedCount > 0 && definedCount < targets.Length;
                _ = toggle.RegisterValueChangedCallback(evt =>
                {
                    toggle.showMixedValue = false;
                    foreach (var target in targets)
                    {
                        PlayerSettings.GetScriptingDefineSymbols(target, out var currentDefines);
                        PlayerSettings.SetScriptingDefineSymbols(target, evt.newValue
                            ? currentDefines.Append(name).Distinct().ToArray()
                            : currentDefines.Where(s => s != name).ToArray());
                    }
                });

                var targetLabel = new Label(usesAllTargets ? "all build targets" : string.Join(", ", targets.Select(target => target.TargetName)));
                targetLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                targetLabel.style.alignSelf = Align.Center;
                targetLabel.style.marginLeft = 4;
                var header = new VisualElement();
                header.style.flexDirection = FlexDirection.Row;
                header.Add(toggle);
                header.Add(targetLabel);

                var description = new Label(define.Description);
                description.style.whiteSpace = WhiteSpace.Normal;
                description.style.marginLeft = 18;
                description.style.marginBottom = 6;
                description.style.opacity = 0.8f;

                var row = new VisualElement();
                row.Add(header);
                row.Add(description);
                return row;
            }

            static NamedBuildTarget? ParseNamedBuildTarget(string targetName)
            {
                if (targetName == NamedBuildTarget.Server.TargetName)
                    return NamedBuildTarget.Server;
                if (Enum.TryParse<BuildTargetGroup>(targetName, out var group) && group != BuildTargetGroup.Unknown)
                    return NamedBuildTarget.FromBuildTargetGroup(group);
                ElympicsLogger.LogWarning($"Unknown build target \"{targetName}\" in an {nameof(ElympicsDefineList)}.");
                return null;
            }
        }
    }
}
