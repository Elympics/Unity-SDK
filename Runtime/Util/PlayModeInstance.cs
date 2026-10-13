#nullable enable

using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using Unity.Multiplayer.PlayMode;
#endif

namespace Elympics
{
    /// <summary>Identifies the Multiplayer Play Mode Editor instance this code runs in.</summary>
    internal static class PlayModeInstance
    {
        private const string NameArgument = "-name";

#if UNITY_EDITOR
        // Constant for the lifetime of the process, so it survives code reloads safely.
        private static bool? isVirtualPlayer;
        private static IReadOnlyList<string>? tags;

        /// <summary>True in an additional Editor instance (virtual player) of Multiplayer Play Mode.</summary>
        public static bool IsVirtualPlayer => isVirtualPlayer ??= ReadCurrentPlayer(() => !CurrentPlayer.IsMainEditor, false);

        /// <summary>The Multiplayer Play Mode tags of this virtual player.</summary>
        public static IReadOnlyList<string> Tags => tags ??= IsVirtualPlayer ? ReadCurrentPlayer(() => CurrentPlayer.Tags, Array.Empty<string>()) : Array.Empty<string>();

        /// <remarks>Without the Multiplayer Play Mode package (6000.3), <see cref="CurrentPlayer"/> throws a NullReferenceException
        /// from its editor implementation. No virtual players can exist then, so this is the main Editor.</remarks>
        private static T ReadCurrentPlayer<T>(Func<T> read, T fallback)
        {
            try
            {
                return read();
            }
            catch (Exception)
            {
                return fallback;
            }
        }
#else
        public static bool IsVirtualPlayer => false;
        public static IReadOnlyList<string> Tags => Array.Empty<string>();
#endif

        /// <summary>The <c>-name</c> Multiplayer Play Mode gives a virtual player (e.g. <c>Player2</c>), or null.</summary>
        public static string? PlayerName => IsVirtualPlayer ? GetPlayerName(Environment.GetCommandLineArgs()) : null;

        internal static string? GetPlayerName(IList<string> commandLine)
        {
            var index = commandLine.IndexOf(NameArgument);
            return index >= 0 && index + 1 < commandLine.Count ? commandLine[index + 1] : null;
        }
    }
}
