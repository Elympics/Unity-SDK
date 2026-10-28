using System;

namespace Elympics.Tests
{
    internal class FakeParameter : Parameter
    {
        public bool Parsed { get; private set; }

        public override Type Type => throw new NotImplementedException();
        protected override void ParseRawValue() => Parsed = true;
        public override string GetValueAsString() => Parsed ? RawValue : null;

        public FakeParameter(string envVarName = null, string queryKey = null, string longArgName = null,
            char? shortArgName = null, int? argIndex = null, Func<string, bool> validate = null)
            : base(envVarName, queryKey, longArgName, shortArgName, argIndex, validate)
        { }
    }
}
