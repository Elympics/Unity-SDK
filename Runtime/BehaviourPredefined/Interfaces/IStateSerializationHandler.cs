namespace Elympics
{
	public interface IStateSerializationHandler : IObservable
	{
		/// <summary>
		/// Called after deserialization of <see cref="ElympicsVar"/>s. May be used e.g. for transferring state from synchronized to non-synchronized variables.
		/// </summary>
		void OnPostStateDeserialize();

		/// <summary>
		/// Called before serialization of <see cref="ElympicsVar"/>s. May be used e.g. for transferring state from non-synchronized to synchronized variables.
		/// </summary>
		void OnPreStateSerialize();
	}
}
