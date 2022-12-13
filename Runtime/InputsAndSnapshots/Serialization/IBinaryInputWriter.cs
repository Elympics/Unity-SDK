namespace Elympics
{
	public interface IBinaryInputWriter : IInputWriter
	{
		void ResetStream();
		byte[] GetData();
	}
}