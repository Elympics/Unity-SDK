using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Elympics
{
    internal abstract class Parameter
    {
        /// <summary>
        /// Greater number means higher priority.
        /// </summary>
        public int Priority { get; private set; }
        public abstract Type Type { get; }
        public ValueSource ValueSource { get; private set; }

        public string EnvironmentVariableName { get; init; }
        public string QueryKey { get; init; }
        public string LongArgumentName { get; init; }
        public char? ShortArgumentName { get; init; }
        public int? ArgumentIndex { get; init; }

        protected bool IsPresent { get; private set; }
        protected string RawValue { get; private set; }

        private readonly Regex _validationRegex;

        protected Parameter(string envVarName = null, string queryKey = null, string longArgName = null,
            char? shortArgName = null, int? argIndex = null, Regex validationRegex = null)
        {
            EnvironmentVariableName = envVarName;
            QueryKey = queryKey;
            LongArgumentName = longArgName;
            ShortArgumentName = shortArgName;
            ArgumentIndex = argIndex;
            _validationRegex = validationRegex;
        }

        public string GetRawValue() => IsPresent ? RawValue : null;

        public void SetRawValue(string value, int valueIndex = 0, ValueSource source = ValueSource.Unknown)
        {
            Priority = valueIndex;
            RawValue = value;
            IsPresent = true;
            ValueSource = source;
        }

        protected void ValidateRawValue()
        {
            if (_validationRegex == null)
                return;
            if (_validationRegex.Match(RawValue).Success)
                return;
            throw new ArgumentException($"Provided value: {RawValue} does not match validation expression: {_validationRegex}");
        }

        public void ValidateAndParseRawValue()
        {
            if (!IsPresent)
                return;
            ValidateRawValue();
            ParseRawValue();
        }

        public virtual void Reset()
        {
            RawValue = null;
            Priority = 0;
            IsPresent = false;
            ValueSource = ValueSource.Unknown;
        }

        protected abstract void ParseRawValue();
        public abstract string GetValueAsString();

        protected static string ToStringInvariant(object value) =>
            value switch
            {
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => $"{value}",
            };
    }

    internal class ValueParameter<T> : Parameter
        where T : struct
    {
        public override Type Type => typeof(T);

        private T? _parsedValue;
        private readonly Func<string, T> _parse;

        public ValueParameter(string envVarName = null, string queryKey = null, string longArgName = null,
            char? shortArgName = null, int? argIndex = null, Regex validationRegex = null, Func<string, T> parse = null)
            : base(envVarName, queryKey, longArgName, shortArgName, argIndex, validationRegex)
        {
            _parse = parse;
            _parse ??= rawValue => (T)Convert.ChangeType(rawValue, typeof(T), CultureInfo.InvariantCulture);
        }

        public T GetValue(T defaultValue) => _parsedValue ?? defaultValue;

        public T? GetValue() => _parsedValue;

        public override void Reset()
        {
            base.Reset();
            _parsedValue = default;
        }

        protected override void ParseRawValue()
        {
            if (!IsPresent)
                return;
            _parsedValue = _parse(RawValue);
        }

        public override string GetValueAsString() => _parsedValue is not null ? ToStringInvariant(_parsedValue) : null;
    }

    internal class Parameter<T> : Parameter
        where T : class
    {
        public override Type Type => typeof(T);

        private T _parsedValue;
        private bool _isParsed;
        private readonly Func<string, T> _parse;

        public Parameter(string envVarName = null, string queryKey = null, string longArgName = null,
            char? shortArgName = null, int? argIndex = null, Regex validationRegex = null, Func<string, T> parse = null)
            : base(envVarName, queryKey, longArgName, shortArgName, argIndex, validationRegex)
        {
            _parse = parse;
            _parse ??= rawValue => (T)Convert.ChangeType(rawValue, typeof(T));
        }

        public T GetValue(T defaultValue = default) => _isParsed ? _parsedValue : defaultValue;

        public override void Reset()
        {
            base.Reset();
            _parsedValue = default;
            _isParsed = false;
        }

        protected override void ParseRawValue()
        {
            if (!IsPresent)
                return;
            _parsedValue = _parse(RawValue);
            _isParsed = true;
        }

        public override string GetValueAsString() => _isParsed ? ToStringInvariant(_parsedValue) : null;
    }
}
