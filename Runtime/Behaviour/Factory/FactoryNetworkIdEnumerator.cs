using System.IO;

namespace Elympics
{
	public class FactoryNetworkIdEnumerator : ElympicsVar
	{
		private readonly NetworkIdEnumerator _enumerator;

		public FactoryNetworkIdEnumerator(int startNetworkId, bool enabledSynchronization = true) : base(enabledSynchronization)
		{
			_enumerator = new NetworkIdEnumerator(startNetworkId);
		}

		public override void Serialize(BinaryWriter bw)
		{
			bw.Write(_enumerator.GetCurrent());
		}

		public override void Deserialize(BinaryReader br, bool ignoreTolerance = false)
		{
			var value = br.ReadInt32();
			_enumerator.MoveTo(value);
		}

		public override bool Equals(BinaryReader br1, BinaryReader br2) => br1.ReadInt32() == br2.ReadInt32();

		public int  GetCurrent()            => _enumerator.GetCurrent();
		public void MoveTo(int to)          => _enumerator.MoveTo(to);
		public int  MoveNextAndGetCurrent() => _enumerator.MoveNextAndGetCurrent();
	}
}
