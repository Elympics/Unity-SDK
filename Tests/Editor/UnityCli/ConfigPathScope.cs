#nullable enable

using System;

namespace Elympics.Editor.Tests.UnityCli
{
    internal sealed class ConfigPathScope : IDisposable
    {
        private readonly string _original = ElympicsConfig.PathInResources;

        public ConfigPathScope(string pathInResources) => ElympicsConfig.PathInResources = pathInResources;

        public static ConfigPathScope Missing() => new("Elympics/NoSuchConfig_" + Guid.NewGuid().ToString("N"));

        public void Dispose() => ElympicsConfig.PathInResources = _original;
    }
}
