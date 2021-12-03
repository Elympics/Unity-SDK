namespace Elympics
{
	internal interface IBinaryInputWriter : IInputWriter
	{
		void ResetStream();
		byte[] GetData();
	}
}