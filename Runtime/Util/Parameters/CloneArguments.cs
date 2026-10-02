#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using System.IO;
using Plugins.Elympics.Plugins.ParrelSync;
using UnityEngine;
#endif

namespace Elympics
{
    /// <summary>
    /// Command-line arguments of a ParrelSync clone Editor, read from its <c>.parrelsyncarg</c> file (the Arguments field of the Clones Manager).
    /// They are collected after environment variables and URL queries and before the real command line, which wins.
    /// </summary>
    internal static class CloneArguments
    {
        public const string FileName = ".parrelsyncarg";

        public static IList<string> Read()
        {
#if UNITY_EDITOR
            if (!ElympicsClonesManager.IsClone())
                return Array.Empty<string>();
            var path = Path.Combine(Application.dataPath, "..", FileName);
            return File.Exists(path) ? Split(File.ReadAllText(path, Encoding.UTF8)) : Array.Empty<string>();
#else
            return Array.Empty<string>();
#endif
        }

        /// <summary>Splits like a command line: by whitespace, keeping double-quoted parts (without the quotes) together.</summary>
        public static List<string> Split(string? text)
        {
            var arguments = new List<string>();
            if (string.IsNullOrEmpty(text))
                return arguments;
            var current = new StringBuilder();
            var inQuotes = false;
            var hasArgument = false;
            foreach (var character in text!)
            {
                if (character == '"')
                {
                    inQuotes = !inQuotes;
                    hasArgument = true;
                }
                else if (char.IsWhiteSpace(character) && !inQuotes)
                {
                    if (hasArgument)
                        arguments.Add(current.ToString());
                    _ = current.Clear();
                    hasArgument = false;
                }
                else
                {
                    _ = current.Append(character);
                    hasArgument = true;
                }
            }
            if (hasArgument)
                arguments.Add(current.ToString());
            return arguments;
        }
    }
}
