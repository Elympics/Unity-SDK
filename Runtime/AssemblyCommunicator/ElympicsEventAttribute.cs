#nullable enable

using System;

namespace Elympics.AssemblyCommunicator
{
    /// <summary>Mandatory attribute for types used as arguments for events raised from <see cref="CrossAssemblyEventBroadcaster"/>.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    internal class ElympicsEventAttribute : Attribute
    {
        public const string ElympicsSDK = "Elympics";

        public readonly string SourceAssemblyName;

        /// <param name="sourceAssemblyName">Name of the assembly that is allowed to raise this event and not allowed to define observers for it.</param>
        public ElympicsEventAttribute(string sourceAssemblyName) => SourceAssemblyName = sourceAssemblyName;
    }
}
