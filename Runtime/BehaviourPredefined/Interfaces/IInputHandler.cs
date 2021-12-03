namespace Elympics
{
	public interface IInputHandler : IObservable
	{
		/// <summary>
		/// Callback for gathering client input.
		/// </summary>
		/// <param name="inputSerializer">Input serializer. Use its <c>Write</c> methods to include data in the sent input.</param>
		/// <seealso cref="GetInputForBot"/>
		/// <seealso cref="ApplyInput"/>
		void GetInputForClient(IInputWriter inputSerializer);

		/// <summary>
		/// Callback for gathering bot input.
		/// </summary>
		/// <param name="inputSerializer">Input serializer. Use its <c>Write</c> methods to include data in the sent input.</param>
		/// <seealso cref="GetInputForClient"/>
		/// <seealso cref="ApplyInput"/>
		void GetInputForBot(IInputWriter inputSerializer);

		/// <summary>
		/// Callback for applying received input.
		/// </summary>
		/// <param name="player">Identifier of a player that sent the input.</param>
		/// <param name="inputDeserializer">Input deserializer. Use its <c>Read</c> methods to parse data from the received input.</param>
		/// <seealso cref="GetInputForClient"/>
		/// <seealso cref="GetInputForBot"/>
		void ApplyInput(ElympicsPlayer player, IInputReader inputDeserializer);
	}
}
