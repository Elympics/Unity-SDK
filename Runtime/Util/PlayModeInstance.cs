#nullable enable

using System;
using System.Collections.Generic;
#if UNITY_EDITOR && ELYMPICS_MPPM
using Unity.Multiplayer.PlayMode;
#endif

namespace Elympics
{
    /// <summary>Identifies the Multiplayer Play Mode Editor instance this code runs in.</summary>
    internal static class PlayModeInstance
    {
        private const string NameArgument = "-name";

        // Constant for the lifetime of the process, so it survives code reloads safely.
        private static string? playerName;

#if UNITY_EDITOR && ELYMPICS_MPPM
        private static bool? isVirtualPlayer;
        private static IReadOnlyList<string>? tags;

        /// <summary>True in an additional Editor instance (virtual player) of Multiplayer Play Mode.</summary>
        public static bool IsVirtualPlayer => isVirtualPlayer ??= ReadCurrentPlayer(() => !CurrentPlayer.IsMainEditor, false);

        public static IReadOnlyList<string> Tags => tags ??= ReadCurrentPlayer(() => CurrentPlayer.Tags, Array.Empty<string>());

        /// <remarks><see cref="CurrentPlayer"/> throws a NullReferenceException from its editor implementation when its
        /// data store is missing (always without the package in 6000.3). No virtual players can exist then, so this is the main Editor.</remarks>
        private static T ReadCurrentPlayer<T>(Func<T> read, T fallback)
        {
            try
            {
                return read();
            }
            catch (NullReferenceException)
            {
                return fallback;
            }
        }
#else
        // Without the Multiplayer Play Mode package, CurrentPlayer exists but throws, and no virtual players can exist.
        public static bool IsVirtualPlayer => false;
        public static IReadOnlyList<string> Tags => Array.Empty<string>();
#endif

        /// <summary>The <c>-name</c> Multiplayer Play Mode gives a virtual player (e.g. <c>Player2</c>), or null.</summary>
        public static string? PlayerName => playerName ??= IsVirtualPlayer ? GetPlayerName(Environment.GetCommandLineArgs()) : null;

        internal static string? GetPlayerName(IList<string> commandLine)
        {
            var index = commandLine.IndexOf(NameArgument);
            return index >= 0 && index + 1 < commandLine.Count ? commandLine[index + 1] : null;
        }
    }
}
