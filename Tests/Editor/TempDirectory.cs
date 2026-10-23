using System;
using System.IO;

namespace Elympics.Editor.Tests
{
    internal sealed class TempDirectory : IDisposable
    {
        public string Path { get; }

        public TempDirectory(string prefix)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), prefix + "_" + System.IO.Path.GetRandomFileName());
            _ = Directory.CreateDirectory(Path);
        }

        public void Dispose() => Directory.Delete(Path, true);
    }
}
