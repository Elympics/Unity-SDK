using System.IO;

namespace Elympics.Editor.Weaving.Extensions
{
    internal static class PathExtensions
    {
        public static string NormalizePath(this string path) =>
            path.Replace(Path.DirectorySeparatorChar, '/');
    }
}
