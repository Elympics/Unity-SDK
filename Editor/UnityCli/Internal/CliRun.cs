using System;
using System.Threading.Tasks;
using Elympics.Cysharp.Threading.Tasks;
using UnityEditor;

namespace Elympics.Editor.UnityCli
{
    internal static class CliRun
    {
        private const string CheckArgumentsFix = "check the command's arguments in its listing (unity command --query elympics).";

        public static object Sync(Func<object> body)
        {
            try
            {
                return body();
            }
            catch (Exception e) when (!IsInContract(e))
            {
                throw Wrap(e);
            }
        }

        public static async Task<object> Async(Func<UniTask<object>> body)
        {
            try
            {
                return await body();
            }
            catch (Exception e) when (!IsInContract(e))
            {
                throw Wrap(e);
            }
        }

        public static void RequireNotPlaying(string reason)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new ElympicsCliException(ElympicsCliErrors.EditorPlaying, reason, "stop Play mode (editor_stop), then retry.");
        }

        private static bool IsInContract(Exception exception) =>
            exception is ElympicsCliException or OperationCanceledException
            || (exception is ArgumentException && exception.Message.StartsWith(ElympicsCliErrors.Prefix, StringComparison.Ordinal));

        private static Exception Wrap(Exception exception) =>
            exception is ArgumentException
                ? ElympicsCliErrors.InvalidArgumentException(exception.Message, CheckArgumentsFix)
                : new ElympicsCliException(ElympicsCliErrors.Internal, $"{exception.GetType().Name}: {exception.Message}", null, exception);
    }
}
