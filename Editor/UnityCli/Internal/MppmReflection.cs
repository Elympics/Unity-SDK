#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Elympics.Editor.UnityCli
{
    /// <summary>Reaches Multiplayer Play Mode's internal virtual-player API; a missing member fails with <see cref="ElympicsCliErrors.MppmUnavailable"/>.</summary>
    internal static class MppmReflection
    {
        private const string AssemblyName = "UnityEditor.MultiplayerModule";
        private const string Namespace = "Unity.Multiplayer.PlayMode.Editor.";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        internal sealed class Members
        {
            public PropertyInfo Players = null!;
            public PropertyInfo PlayerTags = null!;
            public MethodInfo RegisterTag = null!;
            public PropertyInfo Name = null!;
            public PropertyInfo Type = null!;
            public PropertyInfo State = null!;
            public PropertyInfo Tags = null!;
            public MethodInfo Activate = null!;
            public MethodInfo Deactivate = null!;
            public MethodInfo AddTag = null!;
            public MethodInfo RemoveTag = null!;
        }

        public sealed class VirtualPlayer
        {
            public int number;
            public string? name;
            public string? type;
            public string? state;
            public string[] tags = null!;
        }

        private static Members? cached;

        internal static Members? TryResolve(out string? problem)
        {
            problem = null;
            if (cached != null)
                return cached;

            var assembly = LoadedAssemblies.Get().FirstOrDefault(candidate => candidate.GetName().Name == AssemblyName);
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
                Players = playmode.GetProperty("Players", Flags)!,
                PlayerTags = playmode.GetProperty("PlayerTags", Flags)!,
                RegisterTag = tagsType.GetMethod("Add", Flags, null, new[] { typeof(string), tagError }, null)!,
                Name = player.GetProperty("Name", Flags)!,
                Type = player.GetProperty("Type", Flags)!,
                State = player.GetProperty("PlayerState", Flags)!,
                Tags = player.GetProperty("Tags", Flags)!,
                Activate = player.GetMethod("Activate", Flags, null, new[] { activationError, typeof(List<string>) }, null)!,
                Deactivate = player.GetMethod("Deactivate", Flags, null, new[] { activationError }, null)!,
                AddTag = player.GetMethod("AddTag", Flags, null, new[] { typeof(string), tagError }, null)!,
                RemoveTag = player.GetMethod("RemoveTag", Flags, null, new[] { typeof(string), tagError }, null)!,
            };
            var missing = typeof(Members).GetFields().Where(field => field.GetValue(members) == null).Select(field => field.Name).ToList();
            if (missing.Count > 0)
            {
                problem = $"missing members: {string.Join(", ", missing)}";
                return null;
            }
            return cached = members;
        }

        private static Members Require() => TryResolve(out var problem) ?? throw CreateUnavailableException(problem);

        private static ElympicsCliException CreateUnavailableException(string? problem) =>
            new(ElympicsCliErrors.MppmUnavailable, $"Multiplayer Play Mode's virtual players can't be reached: {problem}.",
                "install com.unity.multiplayer.playmode, or manage virtual players in Window > Multiplayer > Multiplayer Play Mode; "
                + "if MPPM is installed, this Unity version changed its internals (report it to Elympics).");

        private static object[] GetPlayerObjects(Members members) =>
            (members.Players.GetValue(null) as Array)?.Cast<object>().ToArray()
            ?? throw CreateUnavailableException("MultiplayerPlaymode.Players is null (is the Multiplayer Play Mode package installed?)");

        public static List<VirtualPlayer> GetPlayers()
        {
            var members = Require();
            return GetPlayerObjects(members).Select((player, index) => new VirtualPlayer
            {
                number = index + 1,
                name = (string?)members.Name.GetValue(player),
                type = members.Type.GetValue(player)?.ToString(),
                state = members.State.GetValue(player)?.ToString(),
                tags = (string[]?)members.Tags.GetValue(player) ?? Array.Empty<string>(),
            }).ToList();
        }

        /// <param name="number">1 is the main Editor; virtual players are 2-4.</param>
        public static void SetActive(int number, bool active)
        {
            var members = Require();
            var player = GetPlayer(members, number);
            var arguments = active ? new object?[] { null, null } : new object?[] { null };
            var succeeded = (bool)Invoke(active ? members.Activate : members.Deactivate, player, arguments)!;
            if (!succeeded)
            {
                var error = arguments[0]?.ToString();
                if (string.IsNullOrEmpty(error) || error == "None")
                    error = "unknown error";
                throw new ElympicsCliException(ElympicsCliErrors.MppmFailed, $"{(active ? "Activating" : "Deactivating")} Player {number} failed: {error}.",
                    error == "CompileErrors" ? "fix the compile errors first (console --level error)." : "open Window > Multiplayer > Multiplayer Play Mode to see the player's state, then retry.");
            }
        }

        /// <summary>Adds the new tags before removing the old ones that start with <paramref name="prefix"/>, so a failure keeps the old ones.</summary>
        public static void ReplaceTags(int number, string prefix, IReadOnlyCollection<string> tags)
        {
            var members = Require();
            var player = GetPlayer(members, number);
            var registry = members.PlayerTags.GetValue(null) ?? throw CreateUnavailableException("MultiplayerPlaymode.PlayerTags is null");
            var current = (string[]?)members.Tags.GetValue(player) ?? Array.Empty<string>();
            foreach (var tag in tags.Where(tag => !current.Contains(tag, StringComparer.OrdinalIgnoreCase)))
            {
                // Makes the tag known to MPPM before it is added; the return value only says whether it was new.
                _ = Invoke(members.RegisterTag, registry, new object?[] { tag, null });
                InvokeTagChange(members.AddTag, player, tag, "Adding", number);
            }
            foreach (var tag in current.Where(tag => tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
                InvokeTagChange(members.RemoveTag, player, tag, "Removing", number);
        }

        private static void InvokeTagChange(MethodInfo method, object player, string tag, string verb, int number)
        {
            var arguments = new object?[] { tag, null };
            if (!(bool)Invoke(method, player, arguments)!)
                throw new ElympicsCliException(ElympicsCliErrors.MppmFailed, $"{verb} the tag {tag} on Player {number} failed: {arguments[1]}.",
                    arguments[1]?.ToString() == "InPlayMode" ? "stop Play mode (editor_stop), then retry." : "check the tag in Window > Multiplayer > Multiplayer Play Mode, then retry.");
        }

        private static object? Invoke(MethodInfo method, object target, object?[] arguments)
        {
            try
            {
                return method.Invoke(target, arguments);
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                throw new ElympicsCliException(ElympicsCliErrors.MppmFailed, $"Multiplayer Play Mode threw from {method.Name}: {e.InnerException.Message}",
                    "open Window > Multiplayer > Multiplayer Play Mode to see the player's state, then retry.");
            }
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
