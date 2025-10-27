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
        private T _oldValue;
        [SerializeField] private T currentValue;
        private bool _valueChanged;

        public T Value
        {
            get => currentValue;
            set
            {
                if (!_valueChanged)
                {
                    _oldValue = currentValue;
                    _valueChanged = true;
                }
                currentValue = value;
            }
        }

        public ElympicsVarEqualityComparer<T> Comparer { get; }

        public delegate void ValueChangedCallback(T lastValue, T newValue);

        /// <summary>Raised when the variable's value changes on the client, taking accuracy tolerance into the account.</summary>
        /// <remarks>
        /// This event is raised at the start of a tick, after receiving inputs, before calling <see cref="IUpdatable.ElympicsUpdate"/>.
        /// Since it depends on the client-side state of the game and snapshots received from the server, it may not be raised for every change on the server side.
        /// For that reason this event should not be used for critical game logic, but rather for auxiliary effects (e.g. sound effects, visual effects).
        /// All gameplay logic should be based on reading the variable's value in <see cref="IUpdatable.ElympicsUpdate"/>.
        /// </remarks>
        public event ValueChangedCallback ValueChanged;

        protected ElympicsVar(T value = default, bool enabledSynchronization = true, ElympicsVarEqualityComparer<T> comparer = null)
            : base(enabledSynchronization)
        {
            currentValue = value;
            Comparer = comparer ?? new ElympicsDefaultEqualityComparer<T>();
        }

        internal override void Commit()
        {
            if (!_valueChanged)
                return;
            _valueChanged = false;
            if (!Equals(_oldValue, currentValue))
                ValueChanged?.Invoke(_oldValue, currentValue);
        }

        public override string ToString() => Value?.ToString() ?? "null";

        [Obsolete("Refrain from using implicit casts to " + nameof(ElympicsVar) + " value as it reduces readability.")]
        public static implicit operator T(ElympicsVar<T> v) => v.Value;

        public override void Deserialize(BinaryReader br, bool ignoreTolerance = false)
        {
            var deserializedValue = DeserializeInternal(br);
            if (ignoreTolerance || !Comparer.Equals(Value, deserializedValue))
                Value = deserializedValue;
        }

        protected abstract T DeserializeInternal(BinaryReader br);
        public override bool Equals(BinaryReader br1, BinaryReader br2, out string difference1, out string difference2)
        {
            var value1 = DeserializeInternal(br1);
            var value2 = DeserializeInternal(br2);
            var areEqual = Comparer.Equals(value1, value2);

            difference1 = areEqual ? string.Empty : value1?.ToString() ?? "null";
            difference2 = areEqual ? string.Empty : value2?.ToString() ?? "null";

            return areEqual;
        }
    }

    /// <summary>
    /// Base class for all variable types synchronized in Elympics snapshots.
    /// </summary>
    [Serializable]
    public abstract class ElympicsVar
    {
        [SerializeField] private bool _enabledSynchronization;

        protected ElympicsVar(bool enabledSynchronization) => _enabledSynchronization = enabledSynchronization;

        public bool EnabledSynchronization => _enabledSynchronization;
        protected IElympics Elympics { get; private set; }

        public abstract void Serialize(BinaryWriter bw);
        public abstract void Deserialize(BinaryReader br, bool ignoreTolerance = false);
        /// <summary>Compares two serialized values read from the provided <see cref="BinaryReader"/>s.</summary>
        /// <param name="difference1">
        /// Will contain a string describing the deserialized (or partially deserialized) value read from
        /// <paramref name="br1"/> if that value was different than the value from <paramref name="br2"/>.
        /// In other cases it will contain an empty string.
        /// </param>
        /// <param name="difference2">
        /// Will contain a string describing the deserialized (or partially deserialized) value read from
        /// <paramref name="br2"/> if that value was different than the value from <paramref name="br1"/>.
        /// In other cases it will contain an empty string.
        /// </param>
        /// <returns>True if the serialized values are considered the same, otherwise false.</returns>
        /// <remarks>
        /// <paramref name="difference1"/> and <paramref name="difference2"/> are intended to be used for logging or debugging purposes.
        /// Their values should represent the deserialized values in a human-readable form and contain the data necessary to understand how
        /// those two values differ. If differenece between the values is detected without fully deseializing them, string representation should
        /// describe the part of the state where the difference was found.
        /// </remarks>
        public abstract bool Equals(BinaryReader br1, BinaryReader br2, out string difference1, out string difference2);

        internal abstract void Commit();

        internal virtual void Initialize(IElympics elympics)
        {
            Elympics = elympics;
        }
    }
}
