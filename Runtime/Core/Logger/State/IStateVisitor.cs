#nullable enable

namespace Elympics.Core.Logger.State
{
    internal interface IStateVisitor
    {
        void ProcessSubstate(string name, string? legacyName = null);
        void ProcessProperty(string name, string value, string? legacyName = null);
    }

    internal static class StateVisitorExtensions
    {
        public static bool ProcessOptionalProperty(this IStateVisitor visitor, string name, string? value, string? legacyName = null)
        {
            var processed = !string.IsNullOrEmpty(value);
            if (processed)
                visitor.ProcessProperty(name, value!, legacyName);
            return processed;
        }
    }

    internal class PassthroughVisitor : IStateVisitor
    {
        public static readonly PassthroughVisitor Instance = new();

        public void ProcessSubstate(string name, string? legacyName = null)
        { }
        public void ProcessProperty(string name, string value, string? legacyName = null)
        { }
    }
}
