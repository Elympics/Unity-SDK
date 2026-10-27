#nullable enable

using UnityEngine.UIElements;

namespace Elympics.Editor.Config
{
    /// <summary>A section another package adds to <see cref="ManageGamesInElympicsWindow" />.</summary>
    internal interface IManageGameSection
    {
        /// <summary>Sections are shown in ascending order.</summary>
        int Order { get; }

        void CreateGUI(VisualElement container, ElympicsConfig config);
    }
}
