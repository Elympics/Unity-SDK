#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Elympics.Editor.Config
{
    /// <summary>Register <see cref="IManageGameSection" />s from an <c>[InitializeOnLoadMethod]</c>.</summary>
    internal static class ManageGamesWindowSections
    {
        private static readonly List<IManageGameSection> Sections = new();

        public static event Action? Changed;

        public static IReadOnlyList<IManageGameSection> All => Sections;

        public static void Register(IManageGameSection section)
        {
            if (Sections.Any(registered => registered.GetType() == section.GetType()))
                return;
            var index = Sections.FindIndex(registered => registered.Order > section.Order);
            Sections.Insert(index < 0 ? Sections.Count : index, section);
            Changed?.Invoke();
        }
    }
}
