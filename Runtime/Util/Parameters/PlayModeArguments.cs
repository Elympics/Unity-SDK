#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Core.Logger;

namespace Elympics
{
    /// <summary>
    /// Arguments of a Multiplayer Play Mode virtual player. By default it joins the half-remote server as a client,
    /// with the player index taken from its <c>-name PlayerN</c> argument (Player2 is index 0).
    /// <c>Elympics:</c> player tags override that: a launch mode (<c>Elympics:halfRemoteServer</c>), a player index
    /// (<c>Elympics:PlayerIndex=2</c>), any long argument (<c>Elympics:half-remote-ip=10.0.0.5</c>)
    /// or <c>Elympics:manual</c>, which leaves the player to the Match Launcher.
    /// They are collected after environment variables and URL queries and before the real command line, which wins.
    /// </summary>
    internal static class PlayModeArguments
    {
        public const string TagPrefix = "Elympics:";
        public const string ManualTag = "manual";
        private const string PlayerIndexKey = "PlayerIndex";
        private const string PlayerNamePrefix = "Player";
        private const int FirstVirtualPlayerNumber = 2;

        public static IList<string> Read()
        {
            return PlayModeInstance.IsVirtualPlayer
                ? Build(Environment.GetCommandLineArgs(), PlayModeInstance.Tags)
                : Array.Empty<string>();
        }

        internal static List<string> Build(IList<string> commandLine, IEnumerable<string> tags)
        {
            var parameters = ApplicationParameters.Parameters;
            var elympicsTags = tags.Where(tag => tag.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(tag => tag[TagPrefix.Length..].Trim())
                .ToList();
            if (elympicsTags.Any(tag => tag.Equals(ManualTag, StringComparison.OrdinalIgnoreCase)))
                return new List<string>();

            var arguments = new List<string> { LongArgument(parameters.LaunchMode.LongArgumentName, LaunchDataFromParameters.HalfRemoteClient) };
            if (GetPlayerNumber(commandLine) is { } playerNumber)
                arguments.Add(LongArgument(parameters.HalfRemotePlayerIndex.LongArgumentName, (playerNumber - FirstVirtualPlayerNumber).ToString()));

            foreach (var tag in elympicsTags)
            {
                var separator = tag.IndexOf('=');
                if (separator < 0)
                {
                    var launchMode = LaunchDataFromParameters.Normalize(tag);
                    if (launchMode != null)
                        arguments.Add(LongArgument(parameters.LaunchMode.LongArgumentName, launchMode));
                    else
                        ElympicsLogger.LogWarning($"Ignoring the Multiplayer Play Mode tag \"{TagPrefix}{tag}\": "
                            + $"expected {ManualTag}, a launch mode ({string.Join(", ", LaunchDataFromParameters.All)}), {PlayerIndexKey}=<index> or <long-argument>=<value>.");
                    continue;
                }

                var key = tag[..separator].Trim();
                var value = tag[(separator + 1)..].Trim();
                var argumentName = key.Equals(PlayerIndexKey, StringComparison.OrdinalIgnoreCase) ? parameters.HalfRemotePlayerIndex.LongArgumentName : key;
                arguments.Add(LongArgument(argumentName, value));
            }
            return arguments;
        }

        /// <returns>N of the <c>-name PlayerN</c> argument that Multiplayer Play Mode passes to its Editor instances.</returns>
        internal static int? GetPlayerNumber(IList<string> commandLine) =>
            PlayModeInstance.GetPlayerName(commandLine) is { } name
            && name.StartsWith(PlayerNamePrefix, StringComparison.Ordinal)
            && int.TryParse(name[PlayerNamePrefix.Length..], out var number)
                ? number
                : null;

        private static string LongArgument(string name, string value) => $"--{name}={value}";
    }
}
