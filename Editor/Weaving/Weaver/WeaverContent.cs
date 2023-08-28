using UnityEngine;

namespace Elympics.Weaver
{
    public static class WeaverContent
    {
        public static readonly GUIContent SettingsWeavedAsesmbliesTitle;
        public static readonly GUIContent SettingsComponentsTitle;
        public static readonly GUIContent SettingsEnalbedToggle;


        static WeaverContent()
        {
            SettingsEnalbedToggle = new GUIContent
            {
                text = "Enabled",
                tooltip = "If true Weaver will run automatically every time a assembly in our weaved assemblies list changes. When off Weaver will not do anything."
            };

            SettingsComponentsTitle = new GUIContent
            {
                text = "Components",
                tooltip = "A list of all the components that will be executed every time one of the weaved assemblies has changed. " +
                " If you don't want the component to run you can remove it using the '-' button."
            };

            SettingsWeavedAsesmbliesTitle = new GUIContent
            {
                text = "Weaved Assemblies",
                tooltip = "The list of all the assemblies that Weaver will process and apply all it's active components too. If an assembly is not listed " +
                "here Weaver will not touch it. Every time on of these assemblies change Weaver starts running."
            };

        }
    }
}
