#nullable enable

using Elympics.Core.Logger;
using UnityEngine;

namespace Elympics
{
    internal static class ElympicsHostBridgeFactory
    {
        internal const string Auto = "auto";
        internal const string WebGl = "webgl";
        internal const string Native = "native";
        internal const string None = "none";

        /// <param name="mode">
        /// Value of the <c>ELYMPICS_HOST_BRIDGE</c> parameter. <c>auto</c> picks the WebGL bridge in browser builds, none in the Editor
        /// (use Tools > Elympics > Match Launcher there) and the native bridge elsewhere, which stays off if its library is missing.
        /// </param>
        public static IElympicsHostBridge? Create(string? mode)
        {
            switch (string.IsNullOrEmpty(mode) ? Auto : mode!.ToLowerInvariant())
            {
                case None:
                    return null;
                case WebGl:
                    return new ElympicsWebGlHostBridge();
                case Native:
                    return new ElympicsNativeHostBridge();
                case Auto:
                    if (Application.isEditor)
                        return null;
                    return Application.platform == RuntimePlatform.WebGLPlayer ? new ElympicsWebGlHostBridge() : new ElympicsNativeHostBridge();
                default:
                    ElympicsLogger.LogError($"Unknown host bridge \"{mode}\". Use {Auto}, {WebGl}, {Native} or {None}. No host bridge will be used.");
                    return null;
            }
        }
    }
}
