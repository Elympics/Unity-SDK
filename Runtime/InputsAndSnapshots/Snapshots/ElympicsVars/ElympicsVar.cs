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
