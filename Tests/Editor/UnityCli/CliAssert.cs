#nullable enable

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elympics.Cysharp.Threading.Tasks;
using Elympics.Editor.UnityCli;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elympics.Editor.Tests.UnityCli
{
    internal static class CliAssert
    {
        /// <summary>Also asserts a non-empty fix hint.</summary>
        public static void HasCodeAndFix(Exception? exception, string code)
        {
            Assert.That(exception, Is.Not.Null, $"Expected {code}.");
            Assert.That(exception!.Message, Does.StartWith(code + ": "));
            var fix = Regex.Match(exception.Message, Regex.Escape(ElympicsCliErrors.FixMarker) + "(.+)$", RegexOptions.Singleline);
            Assert.That(fix.Success && fix.Groups[1].Value.Trim().Length > 0, $"No fix hint in: {exception.Message}");
        }

        public static void Throws(TestDelegate code, string errorCode) => HasCliExceptionWithCodeAndFix(Assert.Catch(code), errorCode);

        public static void ThrowsAsync(Func<Task<object>> command, string errorCode) => HasCliExceptionWithCodeAndFix(Assert.CatchAsync(async () => await command()), errorCode);

        public static async UniTask ThrowsAwaitAsync(Func<UniTask> code, string errorCode) => HasCliExceptionWithCodeAndFix(await CatchAsync(code), errorCode);

        public static string?[] Codes(JObject result, string list = "findings") => result[list]!.Select(finding => (string?)finding["code"]).ToArray();

        private static void HasCliExceptionWithCodeAndFix(Exception? exception, string errorCode)
        {
            Assert.That(exception, Is.InstanceOf<ElympicsCliException>().Or.InstanceOf<ArgumentException>());
            HasCodeAndFix(exception, errorCode);
        }

        /// <summary>Awaits instead of blocking, so UniTask work that needs the player loop can finish.</summary>
        public static async UniTask<Exception?> CatchAsync(Func<UniTask> code)
        {
            try
            {
                await code();
            }
            catch (Exception e)
            {
                return e;
            }
            return null;
        }
    }
}
