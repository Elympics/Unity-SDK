namespace Elympics.Core.Logger.State
{
    internal interface IVisitableState
    {
        /// <summary>
        /// Visits all substates and properties using an instance of <see cref="IStateVisitor"/>.
        /// </summary>
        /// <param name="visitor">The visitor object.</param>
        /// <returns>Was anything visited? May be <c>false</c> when all properties are empty.</returns>
        bool Visit(IStateVisitor visitor);
    }
}
