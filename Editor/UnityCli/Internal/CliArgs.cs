using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Elympics.Editor.UnityCli
{
    internal static class CliArgs
    {
        private const string VersionPattern = "^[0-9-a-zA-Z.]+$";
        private const int MaxVersionLength = 63;

        public static string Require(string value, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw ElympicsCliErrors.InvalidArgumentException($"Missing required argument --{argumentName}.", $"pass --{argumentName} <value>.");
            return value.Trim();
        }

        internal static Func<string> ProjectRoot = () => Path.GetDirectoryName(Application.dataPath);

        /// <summary>Resolves a path relative to the project root. Unless <paramref name="allowExternal"/>, it must stay inside the project.</summary>
        public static string ResolveProjectPath(string path, string argumentName, bool allowExternal)
        {
            var root = Path.GetFullPath(ProjectRoot()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var resolved = Path.GetFullPath(Path.Combine(root, path));
            var inside = resolved.Equals(root, StringComparison.OrdinalIgnoreCase)
                || resolved.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            if (!inside && !allowExternal)
                throw ElympicsCliErrors.InvalidArgumentException($"--{argumentName} {resolved} is outside the project {root}, so its files could be anything on this machine.",
                    $"pass a folder inside the project, or add --allow_external_path true if uploading {resolved} is intended.");
            return resolved;
        }

        public static void RequireConfirm(bool confirm, string action)
        {
            if (!confirm)
                throw new ElympicsCliException(ElympicsCliErrors.ConfirmRequired,
                    $"{action} can't be undone, so it needs an explicit confirmation.",
                    "check the arguments, then repeat the call with --confirm true.");
        }

        public static string ValidateVersion(string version, string argumentName)
        {
            version = Require(version, argumentName);
            if (version.Length > MaxVersionLength || !Regex.IsMatch(version, VersionPattern))
                throw ElympicsCliErrors.InvalidArgumentException(
                    $"--{argumentName} '{version}' is invalid: use 1-{MaxVersionLength} Latin letters, digits, dashes and dots.",
                    $"pass a --{argumentName} such as 1.2.0 or 2026-10-22.1.");
            return version;
        }

        public static string ValidateGuid(string value, string argumentName)
        {
            value = Require(value, argumentName);
            if (!Guid.TryParse(value, out _))
                throw ElympicsCliErrors.InvalidArgumentException($"--{argumentName} '{value}' is not a GUID.",
                    "pass an ID from elympics_games_list (or elympics_queues_list for queues).");
            return value;
        }

        public static string Optional(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        public static bool? ParseOptionalBool(string value, string argumentName) => value?.Trim().ToLowerInvariant() switch
        {
            "auto" or "" or null => null,
            "true" => true,
            "false" => false,
            _ => throw ElympicsCliErrors.InvalidArgumentException($"Unknown --{argumentName} '{value}'.", "use auto, true or false."),
        };

        public static List<string> ParseStringArray(string json, string argumentName) =>
            ParseJsonObject(json, argumentName, "a JSON array of strings", $"pass e.g. --{argumentName} '[\"a\",\"b\"]'.",
                text => JArray.Parse(text).ToObject<List<string>>()) ?? new List<string>();

        public static Dictionary<string, string> ParseStringObject(string json, string argumentName) =>
            ParseJsonObject(json, argumentName, "a JSON object with string values", $"pass e.g. --{argumentName} '{{\"key\":\"value\"}}'.",
                text => JObject.Parse(text).ToObject<Dictionary<string, string>>()) ?? new Dictionary<string, string>();

        /// <returns>Null for blank <paramref name="json"/>, so the caller picks the empty value.</returns>
        public static T ParseJsonObject<T>(string json, string argumentName, string expectation, string fix, Func<string, T> parse) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                return parse(json);
            }
            catch (Exception e) when (e is JsonException or ArgumentException or InvalidCastException)
            {
                throw ElympicsCliErrors.InvalidArgumentException($"--{argumentName} must be {expectation}: {e.Message}", fix);
            }
        }

        public static TEnum ParseEnum<TEnum>(string value, string argumentName) where TEnum : struct, Enum
        {
            value = Require(value, argumentName);
            if (Enum.TryParse<TEnum>(value.Replace("_", string.Empty), true, out var parsed) && Enum.IsDefined(typeof(TEnum), parsed))
                return parsed;
            throw ElympicsCliErrors.InvalidArgumentException($"Unknown --{argumentName} '{value}'.",
                $"use one of: {string.Join(", ", Enum.GetNames(typeof(TEnum)))}.");
        }
    }
}
