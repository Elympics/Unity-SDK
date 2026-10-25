using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Elympics.Editor.UnityCli
{
    /// <summary>
    /// Multiplayer Play Mode has no public API for its virtual players, so its internal one is used through reflection.
    /// A missing member fails closed with <see cref="ElympicsCliErrors.MppmUnavailable"/>.
    /// </summary>
    internal static class MppmReflection
    {
        private const string AssemblyName = "UnityEditor.MultiplayerModule";
        private const string Namespace = "Unity.Multiplayer.PlayMode.Editor.";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        internal sealed class Members
        {
            public PropertyInfo Players;
            public PropertyInfo PlayerTags;
            public MethodInfo RegisterTag;
            public PropertyInfo Name;
            public PropertyInfo Type;
            public PropertyInfo State;
            public PropertyInfo Tags;
            public MethodInfo Activate;
            public MethodInfo Deactivate;
            public MethodInfo AddTag;
            public MethodInfo RemoveTag;
        }

        public sealed class VirtualPlayer
        {
            public int number;
            public string name;
            public string type;
            public string state;
            public string[] tags;
        }

        private static Members cached;

        internal static Members TryResolve(out string problem)
        {
            problem = null;
            if (cached != null)
                return cached;

            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(candidate => candidate.GetName().Name == AssemblyName);
            if (assembly == null)
            {
                problem = $"the {AssemblyName} assembly isn't loaded";
                return null;
            }

            var playmode = assembly.GetType(Namespace + "MultiplayerPlaymode");
            var player = assembly.GetType(Namespace + "UnityPlayer");
            var tagsType = assembly.GetType(Namespace + "UnityPlayerTags");
            var activationError = assembly.GetType(Namespace + "ActivationError")?.MakeByRefType();
            var tagError = assembly.GetType(Namespace + "TagError")?.MakeByRefType();
            if (playmode == null || player == null || tagsType == null || activationError == null || tagError == null)
            {
                problem = "the MultiplayerPlaymode, UnityPlayer, UnityPlayerTags, ActivationError or TagError type is missing";
                return null;
            }

            var members = new Members
            {
                Players = playmode.GetProperty("Players", Flags),
                PlayerTags = playmode.GetProperty("PlayerTags", Flags),
                RegisterTag = tagsType.GetMethod("Add", Flags, null, new[] { typeof(string), tagError }, null),
                Name = player.GetProperty("Name", Flags),
                Type = player.GetProperty("Type", Flags),
                State = player.GetProperty("PlayerState", Flags),
                Tags = player.GetProperty("Tags", Flags),
                Activate = player.GetMethod("Activate", Flags, null, new[] { activationError, typeof(List<string>) }, null),
                Deactivate = player.GetMethod("Deactivate", Flags, null, new[] { activationError }, null),
                AddTag = player.GetMethod("AddTag", Flags, null, new[] { typeof(string), tagError }, null),
                RemoveTag = player.GetMethod("RemoveTag", Flags, null, new[] { typeof(string), tagError }, null),
            };
            var missing = typeof(Members).GetFields().Where(field => field.GetValue(members) == null).Select(field => field.Name).ToList();
            if (missing.Count > 0)
            {
                problem = $"missing members: {string.Join(", ", missing)}";
                return null;
            }
            return cached = members;
        }

        private static Members Require() => TryResolve(out var problem) ?? throw Unavailable(problem);

        private static ElympicsCliException Unavailable(string problem) =>
            new(ElympicsCliErrors.MppmUnavailable, $"Multiplayer Play Mode's virtual players can't be reached: {problem}.",
                "install com.unity.multiplayer.playmode 2.x, or manage virtual players in Window > Multiplayer > Multiplayer Play Mode; "
                + "if MPPM is installed, this Unity version changed its internals (report it to Elympics).");

        private static object[] GetPlayerObjects(Members members) =>
            (members.Players.GetValue(null) as Array)?.Cast<object>().ToArray()
            ?? throw Unavailable("MultiplayerPlaymode.Players is null (is the Multiplayer Play Mode package installed?)");

        public static List<VirtualPlayer> GetPlayers()
        {
            var members = Require();
            return GetPlayerObjects(members).Select((player, index) => new VirtualPlayer
            {
                number = index + 1,
                name = (string)members.Name.GetValue(player),
                type = members.Type.GetValue(player)?.ToString(),
                state = members.State.GetValue(player)?.ToString(),
                tags = (string[])members.Tags.GetValue(player) ?? Array.Empty<string>(),
            }).ToList();
        }

        /// <param name="number">1 is the main Editor; virtual players are 2-4.</param>
        public static void SetActive(int number, bool active)
        {
            var members = Require();
            var player = GetPlayer(members, number);
            var arguments = active ? new object[] { null, null } : new object[] { null };
            var succeeded = (bool)(active ? members.Activate : members.Deactivate).Invoke(player, arguments);
            if (!succeeded && arguments[0]?.ToString() is { } error && error != "None")
                throw new ElympicsCliException(ElympicsCliErrors.MppmFailed, $"{(active ? "Activating" : "Deactivating")} Player {number} failed: {error}.",
                    error == "CompileErrors" ? "fix the compile errors first (console --level error)." : "open Window > Multiplayer > Multiplayer Play Mode to see the player's state, then retry.");
        }

        /// <summary>Adds the new tags before removing the old ones that start with <paramref name="prefix"/>, so a failure keeps the old ones.</summary>
        public static void ReplaceTags(int number, string prefix, IReadOnlyCollection<string> tags)
        {
            var members = Require();
            var player = GetPlayer(members, number);
            var registry = members.PlayerTags.GetValue(null);
            var current = (string[])members.Tags.GetValue(player) ?? Array.Empty<string>();
            foreach (var tag in tags.Where(tag => !current.Contains(tag, StringComparer.OrdinalIgnoreCase)))
            {
                _ = members.RegisterTag.Invoke(registry, new object[] { tag, null });
                Invoke(members.AddTag, player, tag, "Adding", number);
            }
            foreach (var tag in current.Where(tag => tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
                Invoke(members.RemoveTag, player, tag, "Removing", number);
        }

        private static void Invoke(MethodInfo method, object player, string tag, string verb, int number)
        {
            var arguments = new object[] { tag, null };
            if (!(bool)method.Invoke(player, arguments))
                throw new ElympicsCliException(ElympicsCliErrors.MppmFailed, $"{verb} the tag {tag} on Player {number} failed: {arguments[1]}.",
                    arguments[1]?.ToString() == "InPlayMode" ? "stop Play mode (editor_stop), then retry." : "check the tag in Window > Multiplayer > Multiplayer Play Mode, then retry.");
        }

        private static object GetPlayer(Members members, int number)
        {
            var players = GetPlayerObjects(members);
            if (number < 2 || number > players.Length)
                throw ElympicsCliErrors.InvalidArgumentException($"There is no virtual Player {number}.", $"use a player number from 2 to {players.Length}.");
            return players[number - 1];
        }
    }
}
