#nullable enable

using System;

namespace Elympics.Editor.UnityCli
{
    /// <summary>A command failure with a stable code; the pipeline reports only the message, so code and fix are part of it (<see cref="ElympicsCliErrors.Format"/>).</summary>
    internal sealed class ElympicsCliException : InvalidOperationException
    {
        public ElympicsCliException(string code, string message, string? fix, Exception? innerException = null)
            : base(ElympicsCliErrors.Format(code, message, fix), innerException)
        {
            if (string.IsNullOrWhiteSpace(fix) && code != ElympicsCliErrors.Internal)
                throw new ArgumentException($"{code} needs a fix hint.", nameof(fix));
        }
    }
}
