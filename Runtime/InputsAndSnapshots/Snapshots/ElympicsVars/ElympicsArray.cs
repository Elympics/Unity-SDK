using System;
using System.Collections.Generic;
using System.IO;

namespace Elympics
{
	[Serializable]
	public class ElympicsArray<T>: ElympicsVar where T : ElympicsVar
	{
		public IReadOnlyList<T> Values;

		public ElympicsArray(int capacity, Func<T> factory, bool enableSynchronization = true) : base(enableSynchronization)
		{
			Values = Populate(new T[capacity], factory);
		}

		public ElympicsArray(params T[] elems) : this(true, elems)
		{
			Values = elems;
		}

		public ElympicsArray(bool enableSynchronization, params T[] elems) : base(enableSynchronization)
		{
			Values = elems;
		}

		public override void Serialize(BinaryWriter bw)
		{
			foreach (var elympicsVar in Values)
				elympicsVar.Serialize(bw);
		}

		public override void Deserialize(BinaryReader br, bool ignoreTolerance = false)
		{
			foreach (var elympicsVar in Values)
				elympicsVar.Deserialize(br, ignoreTolerance);
		}

		public override bool Equals(BinaryReader br1, BinaryReader br2)
		{
			foreach (var elympicsVar in Values)
			{
				if (!elympicsVar.Equals(br1, br2))
					return false;
			}
			return true;
		}

		private static T[] Populate(T[] array, Func<T> provider)
		{
			for (int i = 0; i < array.Length; i++)
				array[i] = provider();
			return array;
		}

		internal override void Initialize(IElympics elympics)
		{
			base.Initialize(elympics);

			foreach (T element in Values)
				element.Initialize(elympics);
		}
	}
}
