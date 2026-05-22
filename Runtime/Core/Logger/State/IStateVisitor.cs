namespace Elympics.Core.Logger.State
{
    internal interface IStateVisitor
    {
        void ProcessSubstate(string name);
        void ProcessProperty(string name, string value);
    }
}
