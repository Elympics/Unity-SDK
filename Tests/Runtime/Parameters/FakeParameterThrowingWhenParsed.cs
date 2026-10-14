using System;

namespace Elympics.Tests
{
    internal class FakeParameterThrowingWhenParsed : Parameter
    {
        public override Type Type => throw new NotImplementedException();
        protected override void ParseRawValue() => throw new NotImplementedException();
        public override string GetValueAsString() => null;

        public FakeParameterThrowingWhenParsed(string envVarName = null, string queryKey = null, string longArgName = null,
            char? shortArgName = null, int? argIndex = null, Func<string, bool> validate = null)
            : base(envVarName, queryKey, longArgName, shortArgName, argIndex, validate)
        { }
    }
}
