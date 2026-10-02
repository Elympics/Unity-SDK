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
