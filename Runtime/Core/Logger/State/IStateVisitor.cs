#nullable enable

namespace Elympics.Core.Logger.State
{
    internal interface IStateVisitor
    {
        void ProcessSubstate(string name);
        void ProcessProperty(string name, string value);
    }

    internal static class StateVisitorExtensions
    {
        public static void ProcessOptionalProperty(this IStateVisitor visitor, string name, string? value)
        {
            if (!string.IsNullOrEmpty(value))
                visitor.ProcessProperty(name, value!);
        }
    }
}
