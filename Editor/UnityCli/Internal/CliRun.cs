#nullable enable

using System;
using System.Threading.Tasks;
using Elympics.Cysharp.Threading.Tasks;
using UnityEditor;

namespace Elympics.Editor.UnityCli
{
    internal static class CliRun
    {
        private const string CheckArgumentsFix = "check the command's arguments in its listing (unity command --query elympics).";

        public static object Run(Func<object> body)
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

        public static async Task<object> RunAsync(Func<UniTask<object>> body)
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

        public static void RequireNoCompileErrors()
        {
            if (EditorUtility.scriptCompilationFailed)
                throw new ElympicsCliException(ElympicsCliErrors.CompileErrors, "Scripts have compile errors.", "fix the errors listed by console --level error, recompile, then retry.");
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
