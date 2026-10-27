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

        public static IReadOnlyList<IManageGameSection> All => Sections.OrderBy(section => section.Order).ToList();

        public static void Register(IManageGameSection section)
        {
            if (Sections.Any(registered => registered.GetType() == section.GetType()))
                return;
            Sections.Add(section);
            Changed?.Invoke();
        }
    }
}
