using System;
using UnityEditor;

namespace Elympics.Editor.Communication
{
    /// <summary>
    /// The Editor sends the Elympics login only to https Elympics hosts, or to one custom host trusted on this machine (EditorPrefs, never the project), which must use https unless it is localhost.
    /// </summary>
    internal static class ElympicsApiHostPolicy
    {
        internal const string CustomHostPrefsKey = "Elympics/TrustedCustomApiHost";

        private static readonly string[] TrustedDomains = { "elympics.cc", "elympics.ai" };

        internal static Func<string> TrustedCustomHost = () => EditorPrefs.GetString(CustomHostPrefsKey, string.Empty);

        public static bool IsTrusted(Uri uri)
        {
            var host = uri.Host;
            var customHost = TrustedCustomHost();
            if (!string.IsNullOrEmpty(customHost) && string.Equals(host, customHost, StringComparison.OrdinalIgnoreCase))
                return uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && IsLoopback(host));
            if (uri.Scheme != Uri.UriSchemeHttps)
                return false;
            foreach (var domain in TrustedDomains)
                if (string.Equals(host, domain, StringComparison.OrdinalIgnoreCase)
                    || host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static bool IsLoopback(string host) =>
            host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host == "127.0.0.1" || host == "::1" || host == "[::1]";

        public static void Require(Uri uri)
        {
            if (!IsTrusted(uri))
                throw new ElympicsEditorApiException(EditorApiErrorKind.UntrustedHost,
                    $"{uri.Scheme}://{uri.Host} isn't an https Elympics host (*.elympics.cc, *.elympics.ai) or a trusted custom host (https, or http on localhost only), so the Editor won't send the Elympics login to it.",
                    fix: $"correct the API endpoint in ElympicsConfig or its command-line override, or, if you trust this server, use {ElympicsEditorMenuPaths.TrustCustomApiHost}.");
        }

        [MenuItem(ElympicsEditorMenuPaths.TrustCustomApiHost)]
        private static void TrustCustomApiHost()
        {
            const string title = "Trust custom API host";
            var current = TrustedCustomHost();
            var endpoint = ElympicsConfig.Load()?.ElympicsApiEndpoint;
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            {
                _ = EditorUtility.DisplayDialog(title, $"The API endpoint \"{endpoint}\" in ElympicsConfig isn't a valid URL.", "OK");
                return;
            }
            var stored = string.IsNullOrEmpty(current) ? "No custom host is trusted now." : $"Trusted now: {current}.";
            if (IsTrusted(uri))
            {
                var message = $"{uri.Host} is already trusted. {stored}";
                if (string.IsNullOrEmpty(current))
                    _ = EditorUtility.DisplayDialog(title, message, "OK");
                else if (EditorUtility.DisplayDialog(title, message, "Stop trusting " + current, "Close"))
                    EditorPrefs.DeleteKey(CustomHostPrefsKey);
                return;
            }
            if (EditorUtility.DisplayDialog(title,
                    $"Send your Elympics login to {uri.Host}? Only do this for a server you control. This setting applies to every project on this machine "
                    + $"and replaces any earlier trusted host. {stored}", "Trust " + uri.Host, "Cancel"))
                EditorPrefs.SetString(CustomHostPrefsKey, uri.Host);
        }
    }
}
