#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Elympics.Cysharp.Threading.Tasks;
using Elympics.Editor.Communication;

namespace Elympics.Editor.UnityCli
{
    internal static class CliWeb
    {
        public const int DefaultTimeoutSeconds = 20;
        public const string TimeoutDescription = "Seconds to wait for the backend.";
        public const int DefaultUploadTimeoutSeconds = ElympicsEditorApi.FileUploadTimeoutSeconds;
        public const string UploadTimeoutDescription = "Seconds to wait for the whole upload.";
        public const string GameIdDescription = "Elympics game ID. Defaults to the active game config's ID.";
        public const string ConfigGameIdDescription = "Elympics game ID of a game config in ElympicsConfig. Defaults to the active game.";
        public const string UploadConfirmDescription = "Must be true: the upload can't be undone.";

        public const string UploadTimeoutFix = "check the Internet connection and retry with a new version name and a larger --timeout_seconds (and the CLI's --timeout).";
        private const string TimeoutFix = "check the Internet connection and retry with a larger --timeout_seconds (and the CLI's --timeout).";
        private const string LoginFix = "log in via Tools > Elympics > Manage games in Elympics, then retry.";

        public static Task<object> RunAsync(int timeoutSeconds, Func<CancellationToken, UniTask<object>> body, string requestFailedCode = ElympicsCliErrors.RequestFailed,
            string timeoutFix = TimeoutFix) =>
            CliRun.RunAsync(async () =>
            {
                RequirePositiveTimeout(timeoutSeconds);

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                try
                {
                    return await body(timeout.Token);
                }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested)
                {
                    throw new ElympicsCliException(ElympicsCliErrors.NetworkTimeout, $"The Elympics backend didn't answer within {timeoutSeconds} s.", timeoutFix);
                }
                catch (ElympicsEditorApiException e)
                {
                    throw Map(e, requestFailedCode);
                }
            });

        public static void RequirePositiveTimeout(int timeoutSeconds)
        {
            if (timeoutSeconds <= 0)
                throw ElympicsCliErrors.InvalidArgumentException("--timeout_seconds must be positive.", "pass e.g. --timeout_seconds 20.");
        }

        /// <summary>Checks the login (refreshing an expired token) before slow local work.</summary>
        public static async Task EnsureLoggedInAsync(string requestFailedCode = ElympicsCliErrors.RequestFailed) =>
            _ = await RunAsync(DefaultTimeoutSeconds, async ct =>
            {
                await ElympicsEditorApi.EnsureLoggedInAsync(0, DefaultTimeoutSeconds, ct);
                return new { };
            }, requestFailedCode, "check the Internet connection and retry.");

        public static ElympicsCliException Map(ElympicsEditorApiException exception, string requestFailedCode = ElympicsCliErrors.RequestFailed) => exception.Kind switch
        {
            EditorApiErrorKind.NotLoggedIn or EditorApiErrorKind.Unauthorized =>
                new ElympicsCliException(ElympicsCliErrors.NotLoggedIn, exception.Message, LoginFix, exception),
            EditorApiErrorKind.UntrustedHost => new ElympicsCliException(ElympicsCliErrors.UntrustedHost, exception.Problem, exception.Fix, exception),
            _ => ToCliException(exception, requestFailedCode),
        };

        private static ElympicsCliException ToCliException(ElympicsEditorApiException exception, string requestFailedCode)
        {
            var (code, fix) = exception.Kind switch
            {
                EditorApiErrorKind.Forbidden => (ElympicsCliErrors.RequestFailed,
                    "log in with an account that is a member of the game's organization, or pass the ID of a game this account can access."),
                EditorApiErrorKind.NotFound => (ElympicsCliErrors.RequestFailed,
                    "check the IDs: list games with elympics_list_games, versions with elympics_list_game_versions and queues with elympics_list_queues."),
                EditorApiErrorKind.Timeout => (ElympicsCliErrors.NetworkTimeout, "check the Internet connection and retry; for uploads, retry with a new version name."),
                EditorApiErrorKind.Network => (ElympicsCliErrors.RequestFailed, "check the Internet connection and the endpoints reported by elympics_status, then retry."),
                _ => (requestFailedCode, "read the backend's message above, correct the arguments and retry."),
            };
            return new ElympicsCliException(code, exception.Message, fix, exception);
        }
    }
}
