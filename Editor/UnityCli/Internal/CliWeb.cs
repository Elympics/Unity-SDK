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

        /// <summary>
        /// Runs backend calls with the current login and a timeout, mapping web errors to CLI error codes. The login is checked by the first request,
        /// so argument errors are reported before a missing login.
        /// </summary>
        public static Task<object> RunAsync(int timeoutSeconds, Func<CancellationToken, UniTask<object>> body, string requestFailedCode = ElympicsCliErrors.RequestFailed) =>
            CliRun.Async(async () =>
        {
            if (timeoutSeconds <= 0)
                throw ElympicsCliErrors.InvalidArgumentException("--timeout_seconds must be positive.", "pass e.g. --timeout_seconds 20.");

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            try
            {
                return await body(timeout.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                throw new ElympicsCliException(ElympicsCliErrors.NetworkTimeout, $"The Elympics backend didn't answer within {timeoutSeconds} s.",
                    "check the Internet connection and retry with a larger --timeout_seconds (and the CLI's --timeout).");
            }
            catch (ElympicsEditorApiException e)
            {
                throw Map(e, requestFailedCode);
            }
        });

        public static ElympicsCliException Map(ElympicsEditorApiException exception, string requestFailedCode = ElympicsCliErrors.RequestFailed) => exception.Kind switch
        {
            EditorApiErrorKind.NotLoggedIn or EditorApiErrorKind.Unauthorized =>
                new ElympicsCliException(ElympicsCliErrors.NotLoggedIn, exception.Message, CliGames.LoginFix, exception),
            EditorApiErrorKind.Forbidden =>
                new ElympicsCliException(ElympicsCliErrors.RequestFailed, exception.Message,
                    "log in with an account that is a member of the game's organization, or pass the ID of a game this account can access.", exception),
            EditorApiErrorKind.NotFound =>
                new ElympicsCliException(ElympicsCliErrors.RequestFailed, exception.Message,
                    "check the IDs: list games with elympics_games_list, versions with elympics_game_versions and queues with elympics_queues_list.", exception),
            EditorApiErrorKind.Timeout =>
                new ElympicsCliException(ElympicsCliErrors.NetworkTimeout, exception.Message,
                    "check the Internet connection and retry; for uploads, retry with a new version name.", exception),
            EditorApiErrorKind.Network =>
                new ElympicsCliException(ElympicsCliErrors.RequestFailed, exception.Message,
                    "check the Internet connection and the endpoints reported by elympics_status, then retry.", exception),
            _ => new ElympicsCliException(requestFailedCode, exception.Message,
                "read the backend's message above, correct the arguments and retry.", exception),
        };
    }
}
