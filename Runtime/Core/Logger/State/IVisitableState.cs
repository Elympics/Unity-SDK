namespace Elympics.Core.Logger.State
{
    internal interface IVisitableState
    {
        void Visit(IStateVisitor visitor);
    }
}
