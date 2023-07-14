namespace Elympics
{
    public interface IInputHandler : IObservable
    {
        /// <summary>
        /// Callback for gathering client input.
        /// </summary>
        /// <param name="inputSerializer">Input serializer. Use its <c>Write</c> methods to include data in the sent input.</param>
        /// <seealso cref="OnInputForBot"/>
        /// <seealso cref="ElympicsBehaviour.TryGetInput"/>
        void OnInputForClient(IInputWriter inputSerializer);

        /// <summary>
        /// Callback for gathering bot input.
        /// </summary>
        /// <param name="inputSerializer">Input serializer. Use its <c>Write</c> methods to include data in the sent input.</param>
        /// <seealso cref="OnInputForClient"/>
        /// <seealso cref="ElympicsBehaviour.TryGetInput"/>
        void OnInputForBot(IInputWriter inputSerializer);
    }
}
