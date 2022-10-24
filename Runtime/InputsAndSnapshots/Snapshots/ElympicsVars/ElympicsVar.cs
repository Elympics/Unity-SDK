using System;
using System.IO;

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
		private T _oldValue;
		private T _currentValue;
		private bool _valueChanged;

		public T Value
		{
			get => _currentValue;
			set
			{
				if (!_valueChanged)
				{
					_oldValue = _currentValue;
					_valueChanged = true;
				}
				_currentValue = value;
			}
		}

		public ElympicsVarEqualityComparer<T> Comparer { get; }

		public delegate void ValueChangedCallback(T lastValue, T newValue);
		public event ValueChangedCallback ValueChanged;

		protected ElympicsVar(T value = default, bool enabledSynchronization = true, ElympicsVarEqualityComparer<T> comparer = null)
			: base(enabledSynchronization)
		{
			_currentValue = value;
			Comparer = comparer ?? new ElympicsDefaultEqualityComparer<T>();
		}

		internal override void Commit()
		{
			if (!_valueChanged)
				return;
			_valueChanged = false;
			if (!Equals(_oldValue, _currentValue))
				ValueChanged?.Invoke(_oldValue, _currentValue);
		}

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

		internal abstract void Commit();

		internal virtual void Initialize(IElympics elympics)
		{
			Elympics = elympics;
		}
	}
}
