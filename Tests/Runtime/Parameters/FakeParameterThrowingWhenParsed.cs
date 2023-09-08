using System;
using System.Text.RegularExpressions;

namespace Elympics.Tests
{
    internal class FakeParameterThrowingWhenParsed : Parameter
    {
        public override Type Type => throw new NotImplementedException();
        protected override void ParseRawValue() => throw new NotImplementedException();
        public override string GetValueAsString() => null;

        public FakeParameterThrowingWhenParsed(string envVarName = null, string queryKey = null, string longArgName = null,
            char? shortArgName = null, int? argIndex = null, Regex validationRegex = null)
            : base(envVarName, queryKey, longArgName, shortArgName, argIndex, validationRegex)
        { }
    }
}
