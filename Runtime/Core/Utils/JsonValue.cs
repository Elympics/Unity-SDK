using System;
using System.Text;

namespace Elympics.Core.Utils
{
    public enum JsonValueKind
    {
        Null,
        Number,
        Boolean,
        String,
    }

    public readonly struct JsonValue : IEquatable<JsonValue>
    {
        public readonly double NumberValue;
        public readonly bool BooleanValue;
        public readonly string StringValue;
        public readonly JsonValueKind Kind;

        private JsonValue(double n)
        {
            Kind = JsonValueKind.Number;
            NumberValue = n;
            BooleanValue = false;
            StringValue = string.Empty;
        }

        private JsonValue(bool b)
        {
            Kind = JsonValueKind.Boolean;
            BooleanValue = b;
            NumberValue = 0;
            StringValue = string.Empty;
        }

        private JsonValue(string s)
        {
            Kind = JsonValueKind.String;
            StringValue = s;
            NumberValue = 0;
            BooleanValue = false;
        }

        public static JsonValue Number(double n) => new(n);
        public static JsonValue Boolean(bool b) => new(b);
        public static JsonValue String(string s) => new(s);
        public static readonly JsonValue Null = new();

        public static implicit operator JsonValue(sbyte n) => new(n);
        public static implicit operator JsonValue(byte n) => new(n);
        public static implicit operator JsonValue(short n) => new(n);
        public static implicit operator JsonValue(ushort n) => new(n);
        public static implicit operator JsonValue(int n) => new(n);
        public static implicit operator JsonValue(uint n) => new(n);
        public static implicit operator JsonValue(long n) => new(n);
        public static implicit operator JsonValue(ulong n) => new(n);
        public static implicit operator JsonValue(decimal n) => new((double)n);
        public static implicit operator JsonValue(float n) => new(n);
        public static implicit operator JsonValue(double n) => new(n);
        public static implicit operator JsonValue(bool b) => new(b);
        public static implicit operator JsonValue(string s) => new(s);

        internal StringBuilder AppendAsJsonProperty(StringBuilder sb, string key, bool isFirst = false) => Kind switch
        {
            JsonValueKind.Null => sb.AppendJsonProperty(key, null, isFirst),
            JsonValueKind.Number => sb.AppendJsonProperty(key, NumberValue, isFirst),
            JsonValueKind.Boolean => sb.AppendJsonProperty(key, BooleanValue, isFirst),
            JsonValueKind.String => sb.AppendJsonProperty(key, StringValue, isFirst),
            _ => throw new InvalidOperationException("Invalid entry type"),
        };

        public bool Equals(JsonValue other) =>
            Kind == other.Kind
            && Kind switch
            {
                JsonValueKind.Null => true,
                JsonValueKind.Number => NumberValue.Equals(other.NumberValue),
                JsonValueKind.Boolean => BooleanValue == other.BooleanValue,
                JsonValueKind.String => StringValue == other.StringValue,
                _ => NumberValue.Equals(other.NumberValue) && BooleanValue == other.BooleanValue && StringValue == other.StringValue,
            };

        public override bool Equals(object obj) => obj is JsonValue other && Equals(other);

        public override int GetHashCode() => Kind switch
        {
            JsonValueKind.Null => Kind.GetHashCode(),
            JsonValueKind.Number => HashCode.Combine((int)Kind, NumberValue),
            JsonValueKind.Boolean => HashCode.Combine((int)Kind, BooleanValue),
            JsonValueKind.String => HashCode.Combine((int)Kind, StringValue),
            _ => HashCode.Combine((int)Kind, NumberValue, BooleanValue, StringValue),
        };
    }
}
