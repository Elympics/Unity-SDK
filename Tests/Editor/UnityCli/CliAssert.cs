#nullable enable

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elympics.Cysharp.Threading.Tasks;
using Elympics.Editor.UnityCli;
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

        public static void Throws(TestDelegate code, string errorCode)
        {
            var exception = Assert.Catch(code);
            Assert.That(exception, Is.InstanceOf<ElympicsCliException>().Or.InstanceOf<ArgumentException>());
            HasCodeAndFix(exception, errorCode);
        }

        public static void ThrowsAsync(Func<Task<object>> command, string errorCode) => HasCodeAndFix(Assert.CatchAsync(async () => await command()), errorCode);

        public static void ThrowsAsync(Func<UniTask> code, string errorCode) => HasCodeAndFix(Assert.CatchAsync(async () => await code()), errorCode);
    }
}
