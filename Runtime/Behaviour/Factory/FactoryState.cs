using System.Collections.Generic;
using System.IO;

namespace Elympics
{
	public class FactoryState : IElympicsSerializable
	{
		public List<KeyValuePair<int, byte[]>> Parts;

		void IElympicsSerializable.Serialize(BinaryWriter bw)
		{
			bw.Write(Parts);
		}

		void IElympicsSerializable.Deserialize(BinaryReader br)
		{
			Parts = br.ReadListWithKvpIntToByteArray();
		}
	}
}