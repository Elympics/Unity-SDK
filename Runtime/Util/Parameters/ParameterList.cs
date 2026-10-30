using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Elympics.Core.Logger;

namespace Elympics
{
    internal class ParameterList
    {
        public bool Initialized { get; private set; }


        public readonly ValueParameter<bool> IsElympicsOnlineServer = new("ELYMPICS", parse: ParseBoolFlag);

        public readonly ValueParameter<int> HalfRemotePlayerIndex =
            new("ELYMPICS_HALF_REMOTE_PLAYER_INDEX", "playerIndex", "half-remote-player-index");
        public readonly Parameter<IPAddress> HalfRemoteIp = new("ELYMPICS_HALF_REMOTE_IP", "ip", "half-remote-ip",
            parse: IPAddress.Parse, validate: IsDottedQuad)
        { ExpectedFormat = "an IPv4 address" };
        public readonly ValueParameter<ushort> HalfRemotePort = new("ELYMPICS_HALF_REMOTE_PORT", "port", "half-remote-port");

        public readonly Parameter<Uri> ApiEndpoint = new("ELYMPICS_API", "elympicsApi", "elympics-api",
            parse: ParseUri);
        public readonly Parameter<Uri> GameServersEndpoint = new("ELYMPICS_GS", "elympicsGs", "elympics-gs",
            parse: ParseUri);

        // Read by the PlayPad lobby.
        public readonly Parameter<Uri> LobbyEndpoint = new("ELYMPICS_LOBBY", "elympicsLobby", "elympics-lobby",
            parse: ParseUri);
        public readonly Parameter<Uri> AuthEndpoint = new("ELYMPICS_AUTH", "elympicsAuth", "elympics-auth",
            parse: ParseUri);
        public readonly Parameter<string> ClientSecret =
            new("ELYMPICS_CLIENT_SECRET", "elympicsClientSecret", "elympics-client-secret");

        public readonly Parameter<Uri> ReplaySource = new("ELYMPICS_REPLAY", "elympicsReplay", "elympics-replay",
            parse: ParseUri);

        public readonly ValueParameter<bool> ShouldUseWebRtc = new("ELYMPICS_USE_WEBRTC", "elympicsUseWebrtc", "elympics-use-webrtc",
            parse: ParseBoolFlag);
        public readonly ValueParameter<bool> ShouldUseTcpUdp = new("ELYMPICS_USE_TCP_UDP", "elympicsUseTcpUdp", "elympics-use-tcp-udp",
            parse: ParseBoolFlag);

        public readonly Parameter<string> HostBridge = new("ELYMPICS_HOST_BRIDGE", "elympicsHostBridge", "elympics-host-bridge",
            validate: HostBridgeValues.Contains)
        { ExpectedFormat = $"one of: {string.Join(", ", HostBridgeValues)}" };

        /// <summary>
        /// Starts this match in the gameplay scene, without a launcher session: halfRemoteClient, halfRemoteServer, singlePlayer or snapshotReplay.
        /// Limits the process to that one match. See <c>LaunchDataFromParameters</c>.
        /// </summary>
        public readonly Parameter<string> LaunchMode = new("ELYMPICS_LAUNCH_MODE", "elympicsLaunchMode", "elympics-launch-mode");

        /// <summary>The replay of <see cref="LaunchMode"/> snapshotReplay: a path or an http(s) URL. Defaults to the game config's snapshot file path.</summary>
        public readonly Parameter<string> ReplayPath = new("ELYMPICS_REPLAY_PATH", "elympicsReplayPath", "elympics-replay-path");

        /// <summary>
        /// Custom matchmaking data of a half-remote match, e.g. <c>:pub:seed=42;mode=fast</c>. Empty means unset.
        /// </summary>
        public readonly Parameter<IReadOnlyDictionary<string, string>> CustomMatchmakingData = new("ELYMPICS_CUSTOM_MATCHMAKING_DATA",
            "elympicsCustomMatchmakingData", "elympics-custom-matchmaking-data", validate: value => TryParseKeyValuePairs(value, out _),
            parse: value => TryParseKeyValuePairs(value, out var pairs) ? pairs : throw new ArgumentException($"Invalid key=value pairs: {value}"))
        { ExpectedFormat = "key=value pairs separated by ';', each part percent-encoded" };

        private static readonly HashSet<string> HostBridgeValues = new(StringComparer.OrdinalIgnoreCase)
        {
            ElympicsHostBridgeFactory.Auto,
            ElympicsHostBridgeFactory.WebGl,
            ElympicsHostBridgeFactory.Native,
            ElympicsHostBridgeFactory.None,
        };

        // Not a Regex: it would link System.Text.RegularExpressions into WebGL players.
        private static bool IsDottedQuad(string value)
        {
            var parts = value.Split('.');
            return parts.Length == 4 && parts.All(part => part.Length > 0 && part.All(char.IsDigit));
        }

        /// <summary>Parses <c>key=value;key=value</c> with percent-encoded parts. An empty value gives null pairs.</summary>
        internal static bool TryParseKeyValuePairs(string value, out IReadOnlyDictionary<string, string> pairs)
        {
            pairs = null;
            if (value.Length == 0)
                return true;
            var result = new Dictionary<string, string>();
            pairs = result;
            foreach (var entry in value.Split(';'))
            {
                var equalsIndex = entry.IndexOf('=');
                if (equalsIndex <= 0)
                    return false;
                var key = Uri.UnescapeDataString(entry[..equalsIndex]);
                if (key.Length == 0 || result.ContainsKey(key))
                    return false;
                result[key] = Uri.UnescapeDataString(entry[(equalsIndex + 1)..]);
            }
            return true;
        }

        /// <summary>The inverse of <see cref="TryParseKeyValuePairs"/>.</summary>
        internal static string FormatKeyValuePairs(IReadOnlyDictionary<string, string> pairs) =>
            string.Join(";", pairs.Select(pair => $"{EscapePairPart(pair.Key)}={EscapePairPart(pair.Value)}"));

        private static string EscapePairPart(string part) => part.Replace("%", "%25").Replace(";", "%3B").Replace("=", "%3D");

        private static bool TryParseShortArgument(string arg, out char key)
        {
            key = arg.Length == 2 ? arg[1] : default;
            return arg.Length == 2 && arg[0] == '-' && arg[1] != '-';
        }

        /// <summary>Matches <c>--key</c> and <c>--key=value</c>; the key doesn't start with a dash, and the value is everything after the first <c>=</c>.</summary>
        private static bool TryParseLongArgument(string arg, out string key, out string value)
        {
            key = value = null;
            if (arg.Length < 3 || !arg.StartsWith("--", StringComparison.Ordinal) || arg[2] == '-')
                return false;
            var equalsIndex = arg.IndexOf('=', 3);
            if (equalsIndex < 0)
            {
                key = arg[2..];
                return true;
            }
            key = arg[2..equalsIndex];
            value = arg[(equalsIndex + 1)..];
            return true;
        }

        private static bool ParseBoolFlag(string rawValue)
        {
            if (bool.TryParse(rawValue, out var parsedValue))
                return parsedValue;
            return true;  // for handling flags
        }

        private static Uri ParseUri(string rawValue) => new UriBuilder(rawValue).Uri;

        /// <param name="playModeArgs">Named arguments of a Multiplayer Play Mode virtual player (see <see cref="PlayModeArguments"/>). They beat environment variables
        /// and URL queries and lose to <paramref name="args"/>. They do not count for positional arguments.</param>
        public bool Initialize(IDictionary envVariables, IList<string> args, NameValueCollection urlQuery, NameValueCollection elympicsUrlQuery,
            IList<string> playModeArgs = null)
        {
            var (parameters, names) = FindParameters();

            EnsureThereAreNoDuplicatedParameterKeys(parameters, names);

            foreach (var parameter in parameters)
                parameter.Reset();

            CollectFromEnvironmentVariables(parameters, envVariables);
            CollectFromUrlQuery(parameters, urlQuery);
            CollectFromUrlQuery(parameters, elympicsUrlQuery);
            if (playModeArgs != null)
                CollectFromNamedArguments(parameters, playModeArgs);
            CollectFromPositionalArguments(parameters, args);
            CollectFromNamedArguments(parameters, args);

            if (!TryParseAllParameters(parameters, names))
                return false;

            LogResults(parameters, names);
            Initialized = true;
            return true;
        }

        private struct ParametersAndNames
        {
            public Parameter[] Parameters;
            public Dictionary<Parameter, string> Names;

            public void Deconstruct(out Parameter[] parameters, out Dictionary<Parameter, string> names)
            {
                parameters = Parameters;
                names = Names;
            }
        }

        private ParametersAndNames FindParameters()
        {
            var foundFields = GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(field => (field.Name, Value: field.GetValue(this) as Parameter))
                .Where(field => field.Value is not null)
                .ToArray();

            var parameters = foundFields.Select(x => x.Value).ToArray();
            var names = foundFields.ToDictionary(f => f.Value, f => f.Name);
            return new ParametersAndNames { Parameters = parameters, Names = names };
        }

        internal static void CollectFromEnvironmentVariables(Parameter[] parameters, IDictionary envVariables)
        {
            foreach (var envVar in envVariables.Keys)
                parameters.FirstOrDefault(p => p.EnvironmentVariableName == (string)envVar)?
                    .SetRawValue((string)envVariables[envVar], source: ValueSource.EnvironmentVariable);
        }

        internal static void CollectFromUrlQuery(Parameter[] parameters, NameValueCollection urlQuery)
        {
            for (var i = 0; i < urlQuery.AllKeys.Length; i++)
            {
                var key = urlQuery.AllKeys[i];
                var value = urlQuery[i].Split(',')[^1];
                parameters.FirstOrDefault(p => p.QueryKey == key)?
                    .SetRawValue(value, i, ValueSource.UrlQuery);
            }
        }

        internal static void CollectFromPositionalArguments(Parameter[] parameters, IList<string> args)
        {
            var positionalArgs = parameters.Where(p => p.ArgumentIndex is not null)
                .Select(p => (p, p.ArgumentIndex.Value));
            foreach (var (parameter, argIndex) in positionalArgs)
                if (argIndex < args.Count)
                    parameter.SetRawValue(args[argIndex], argIndex, ValueSource.PositionalCommandLineArgument);
        }

        internal static void CollectFromNamedArguments(Parameter[] parameters, IList<string> args)
        {
            for (var i = 0; i < args.Count; i++)
            {
                var value = i + 1 < args.Count ? args[i + 1] : "";

                if (TryParseShortArgument(args[i], out var shortArgKey))
                {
                    parameters.FirstOrDefault(p => p.ShortArgumentName == shortArgKey)?
                        .SetRawValue(value, i, ValueSource.ShortCommandLineArgument);
                    continue;
                }

                if (!TryParseLongArgument(args[i], out var longArgKey, out var inlineValue))
                    continue;
                parameters.FirstOrDefault(p => p.LongArgumentName == longArgKey)?
                    .SetRawValue(inlineValue ?? value, i, ValueSource.LongCommandLineArgument);
            }
        }

        internal static void EnsureThereAreNoDuplicatedParameterKeys(Parameter[] parameters,
            Dictionary<Parameter, string> names)
        {
            var keyToParameterName = new Dictionary<string, string>();
            var duplicateFound = false;

            CheckDuplicatesFor("environment variable", parameters
                .Select(x => (Name: names[x], Key: x.EnvironmentVariableName))
                .Where(x => x.Key is not null));
            CheckDuplicatesFor("URL query key", parameters.Select(x => (Name: names[x], Key: x.QueryKey))
                .Where(x => x.Key is not null));
            CheckDuplicatesFor("argument index", parameters
                .Select(x => (Name: names[x], Key: x.ArgumentIndex?.ToString()))
                .Where(x => x.Key is not null));
            CheckDuplicatesFor("short argument name", parameters
                .Select(x => (Name: names[x], Key: x.ShortArgumentName?.ToString()))
                .Where(x => x.Key is not null));
            CheckDuplicatesFor("long argument name", parameters.Select(x => (Name: names[x], Key: x.LongArgumentName))
                .Where(x => x.Key is not null));

            if (duplicateFound)
                throw new ElympicsException("Duplicated keys found in defined application parameters.");

            void CheckDuplicatesFor(string keyDescription, IEnumerable<(string Name, string Key)> namedKeys)
            {
                keyToParameterName.Clear();
                foreach (var (name, key) in namedKeys)
                {
                    if (!keyToParameterName.ContainsKey(key))
                    {
                        keyToParameterName[key] = name;
                        continue;
                    }

                    ElympicsLogger.LogError($"Duplicated {keyDescription}: {key} for parameter {name}.\n"
                        + $"Previously used in parameter {keyToParameterName[key]}");
                    duplicateFound = true;
                }
            }
        }

        internal static bool TryParseAllParameters(Parameter[] parameters, Dictionary<Parameter, string> names)
        {
            var success = true;
            foreach (var parameter in parameters)
                try
                {
                    parameter.ValidateAndParseRawValue();
                }
                catch (Exception e)
                {
                    success = false;
                    ElympicsLogger.LogException(new ElympicsException("Error parsing the value of application parameter "
                        + $"{names[parameter]}. Source: {parameter.ValueSource}, priority: {parameter.Priority}, "
                        + $"value: {parameter.GetRawValue()}", e));
                }

            return success;
        }

        internal static void LogResults(Parameter[] parameters, Dictionary<Parameter, string> names)
        {
            var sb = new StringBuilder("Processed application parameters:");
            foreach (var parameter in parameters)
                _ = sb.Append($"\n{names[parameter]}: {parameter.GetValueAsString() ?? "null"} (priority {parameter.Priority})");
            ElympicsLogger.LogInfo(sb.ToString());
        }
    }
}
