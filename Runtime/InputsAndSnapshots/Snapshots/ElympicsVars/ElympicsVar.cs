using System;
using System.IO;
using UnityEngine;

namespace Elympics
{
	/// <summary>
	/// Generic base class for all variable types synchronized in Elympics snapshots.
	/// </summary>
	/// <typeparam name="T">Synchronized variable type.</typeparam>
	[Serializable]
	public abstract class ElympicsVar<T> : ElympicsVar
		where T : IEquatable<T>
	{
		public T Value
		{
			get => currentValue;
			set
			{
				var oldValue = currentValue;
				currentValue = value;
				if (!Equals(oldValue, currentValue))
					ValueChanged?.Invoke(oldValue, currentValue);
			}
		}

		[SerializeField] private T currentValue;

		protected ElympicsVar(T value = default, bool enabledSynchronization = true, ElympicsVarEqualityComparer<T> comparer = null)
			: base(enabledSynchronization)
		{
			currentValue = value;
			Comparer = comparer ?? new ElympicsDefaultEqualityComparer<T>();
		}

		public ElympicsVarEqualityComparer<T> Comparer { get; }

		public delegate void ValueChangedCallback(T lastValue, T newValue);

		public event ValueChangedCallback ValueChanged;

		public override string ToString() => Value.ToString();

		public static implicit operator T(ElympicsVar<T> v) => v.Value;

		public override void Deserialize(BinaryReader br, bool ignoreTolerance = false)
		{
			var deserializedValue = DeserializeInternal(br);
			if (ignoreTolerance || !Comparer.Equals(Value, deserializedValue))
				Value = deserializedValue;
		}

		protected abstract T DeserializeInternal(BinaryReader br);
		public override bool Equals(BinaryReader br1, BinaryReader br2) => Comparer.Equals(DeserializeInternal(br1), DeserializeInternal(br2));
	}

	/// <summary>
	/// Base class for all variable types synchronized in Elympics snapshots.
	/// </summary>
	[Serializable]
	public abstract class ElympicsVar
	{
		protected ElympicsVar(bool enabledSynchronization)
		{
			EnabledSynchronization = enabledSynchronization;
		}

		public bool EnabledSynchronization { get; private set; }
		protected IElympics Elympics { get; private set; }

		public abstract void Serialize(BinaryWriter bw);
		public abstract void Deserialize(BinaryReader br, bool ignoreTolerance = false);
		public abstract bool Equals(BinaryReader br1, BinaryReader br2);
		internal virtual void Initialize(IElympics elympics)
		{
			this.Elympics = elympics;
		}
	}
}
