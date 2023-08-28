using UnityEditor;
using UnityEngine;

namespace Elympics.Weaver
{
    [InitializeOnLoad]
    internal class Constants
    {
        /// <summary>
        /// Gives us access to the data path that can be accessed off the main thread. 
        /// </summary>
        public static readonly string DataPath;

        /// <summary>
        /// The root path to the project not including the '/Assets' part ending with a slash
        /// </summary>
        public static readonly string ProjectRoot;

        static Constants()
        {
            DataPath = Application.dataPath;
            ProjectRoot = DataPath[..^6];
        }
    }
}
