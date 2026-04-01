using System.IO;

namespace Elympics.Editor.Weaving.Extensions
{
    internal static class PathUtil
    {
        public static string Normalize(string path) =>
            path.Replace(Path.DirectorySeparatorChar, '/');
    }
}
