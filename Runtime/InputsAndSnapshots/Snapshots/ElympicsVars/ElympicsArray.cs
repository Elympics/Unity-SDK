using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Elympics
{
	[Serializable]
	public class ElympicsArray<T>: ElympicsVar where T : ElympicsVar
	{
		[SerializeField] private T[] values;
		public IReadOnlyList<T> Values => values;

		public ElympicsArray(int capacity, Func<T> factory, bool enableSynchronization = true) : base(enableSynchronization)
		{
			values = Populate(new T[capacity], factory);
		}

		public ElympicsArray(params T[] elems) : this(true, elems)
		{
			values = elems;
		}

		public ElympicsArray(bool enableSynchronization, params T[] elems) : base(enableSynchronization)
		{
			values = elems;
		}

		public override void Serialize(BinaryWriter bw)
		{
			foreach (var elympicsVar in values)
				elympicsVar.Serialize(bw);
		}

		public override void Deserialize(BinaryReader br, bool ignoreTolerance = false)
		{
			foreach (var elympicsVar in values)
				elympicsVar.Deserialize(br, ignoreTolerance);
		}

		public override bool Equals(BinaryReader br1, BinaryReader br2)
		{
			foreach (var elympicsVar in values)
			{
				if (!elympicsVar.Equals(br1, br2))
					return false;
			}
			return true;
		}

		internal override void Commit()
		{
			foreach (var elympicsVar in values)
				elympicsVar.Commit();
		}

		private static T[] Populate(T[] array, Func<T> provider)
		{
			for (var i = 0; i < array.Length; i++)
				array[i] = provider();
			return array;
		}

		internal override void Initialize(IElympics elympics)
		{
			base.Initialize(elympics);

			foreach (var element in values)
				element.Initialize(elympics);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append($"Array({values.Length}) [");
			var first = true;
			foreach (var elympicsVar in values)
			{
				if (!first) sb.Append(", ");
				first = false;
				sb.Append(elympicsVar);
			}

			sb.Append("]");
			return sb.ToString();
		}
	}
}
