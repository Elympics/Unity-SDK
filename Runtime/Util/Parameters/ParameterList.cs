using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Elympics
{
    internal class ParameterList
    {
        public bool Initialized { get; private set; }

        public readonly ValueParameter<bool> IsElympicsOnlineServer = new("ELYMPICS", parse: ParseBoolFlag);
        public readonly ValueParameter<bool> IsElympicsOnlineBot = new("ELYMPICS_BOT", parse: ParseBoolFlag);

        public readonly ValueParameter<int> HalfRemotePlayerIndex =
            new("ELYMPICS_HALF_REMOTE_PLAYER_INDEX", "playerIndex", "half-remote-player-index");
        public readonly Parameter<IPAddress> HalfRemoteIp = new("ELYMPICS_HALF_REMOTE_IP", "ip", "half-remote-ip",
            parse: IPAddress.Parse, validationRegex: new Regex(@"\d+\.\d+\.\d+\.\d+", RegexOptions.Compiled));
        public readonly ValueParameter<ushort> HalfRemotePort = new("ELYMPICS_HALF_REMOTE_PORT", "port", "half-remote-port");

        public readonly Parameter<Uri> ApiEndpoint = new("ELYMPICS_API", "elympicsApi", "elympics-api",
            parse: ParseUri);
        public readonly Parameter<Uri> LobbyEndpoint = new("ELYMPICS_LOBBY", "elympicsLobby", "elympics-lobby",
            parse: ParseUri);
        public readonly Parameter<Uri> AuthEndpoint = new("ELYMPICS_AUTH", "elympicsAuth", "elympics-auth",
            parse: ParseUri);
        public readonly Parameter<Uri> LeaderboardsEndpoint = new("ELYMPICS_LEADERBOARDS", "elympicsLeaderboards",
            "elympics-leaderboards",
            parse: ParseUri);
        public readonly Parameter<Uri> GameServersEndpoint = new("ELYMPICS_GS", "elympicsGs", "elympics-gs",
            parse: ParseUri);

        public readonly ValueParameter<bool> ShouldUseWebRtc = new("ELYMPICS_USE_WEBRTC", null, "elympics-use-webrtc",
            parse: ParseBoolFlag);
        public readonly ValueParameter<bool> ShouldUseTcpUdp = new("ELYMPICS_USE_TCP_UDP", null, "elympics-use-tcp-udp",
            parse: ParseBoolFlag);

        public readonly Parameter<string> ClientSecret =
            new("ELYMPICS_CLIENT_SECRET", "elympicsClientSecret", "elympics-client-secret");
        public readonly ValueParameter<Guid> GameId = new("ELYMPICS_GAME_ID", "elympicsGameId", "elympics-game-id",
            parse: Guid.Parse);
        public readonly Parameter<string> VersionName = new("ELYMPICS_GAME_VERSION", "elympicsGameVersion",
            "elympics-game-version",
            validationRegex: new Regex(".+", RegexOptions.Compiled));
        public readonly Parameter<string> Queue = new("ELYMPICS_QUEUE", "elympicsQueue", "elympics-queue");
        public readonly Parameter<string> Region = new("ELYMPICS_REGION", "elympicsRegion", "elympics-region");

        private static readonly Regex ShortArgRegex = new("^-([^-])$", RegexOptions.Compiled);
        private static readonly Regex LongArgRegex = new("^--([^-][^=]*)(=([^=]*))?$", RegexOptions.Compiled);

        private static bool ParseBoolFlag(string rawValue)
        {
            if (bool.TryParse(rawValue, out var parsedValue))
                return parsedValue;
            return true;  // for handling flags
        }

        private static Uri ParseUri(string rawValue) => new UriBuilder(rawValue).Uri;

        public bool Initialize(IDictionary envVariables, IList<string> args, NameValueCollection urlQuery)
        {
            var (parameters, names) = FindParameters();

            EnsureThereAreNoDuplicatedParameterKeys(parameters, names);

            foreach (var parameter in parameters)
                parameter.Reset();

            CollectFromEnvironmentVariables(parameters, envVariables);
            CollectFromUrlQuery(parameters, urlQuery);
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

                var shortArgMatch = ShortArgRegex.Match(args[i]);
                if (shortArgMatch.Success)
                {
                    var shortArgKey = shortArgMatch.Groups[1].Value[0];
                    parameters.FirstOrDefault(p => p.ShortArgumentName == shortArgKey)?
                        .SetRawValue(value, i, ValueSource.ShortCommandLineArgument);
                    continue;
                }

                var longArgMatch = LongArgRegex.Match(args[i]);
                if (!longArgMatch.Success)
                    continue;
                {
                    var longArgKey = longArgMatch.Groups[1].Value;
                    if (longArgMatch.Groups[3].Success)
                        value = longArgMatch.Groups[3].Value;
                    parameters.FirstOrDefault(p => p.LongArgumentName == longArgKey)?
                        .SetRawValue(value, i, ValueSource.LongCommandLineArgument);
                }
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
                    _ = ElympicsLogger.LogException("Error parsing the value of application parameter "
                        + $"{names[parameter]}. Source: {parameter.ValueSource}, priority: {parameter.Priority}, "
                        + $"value: {parameter.GetRawValue()}", e);
                }

            return success;
        }

        internal static void LogResults(Parameter[] parameters, Dictionary<Parameter, string> names)
        {
            var sb = new StringBuilder("Processed application parameters:");
            foreach (var parameter in parameters)
                _ = sb.Append($"\n{names[parameter]}: {parameter.GetValueAsString() ?? "null"} (priority {parameter.Priority})");
            ElympicsLogger.Log(sb.ToString());
        }
    }
}
