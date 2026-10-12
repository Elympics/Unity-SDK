#nullable enable

using System;
using JetBrains.Annotations;

namespace Elympics.Weaving
{
    [AttributeUsage(AttributeTargets.Assembly)]
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public class ProcessedByElympicsAttribute : Attribute
    {
        public string? Version { get; }

        public ProcessedByElympicsAttribute()
        { }

        public ProcessedByElympicsAttribute(string version) => Version = version;
    }
}
