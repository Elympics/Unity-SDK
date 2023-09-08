using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Elympics.Tests
{
    [Category("Parameters")]
    [TestFixture]
    internal class TestParameter
    {
        private static List<Parameter> EmptyParameters => new()
        {
            new ValueParameter<bool>(parse: _ => true),
            new Parameter<string>(parse: _ => ""),
        };

        [Test]
        public void EmptyParameterShouldHaveNoValueAndZeroPriority([ValueSource(nameof(EmptyParameters))] Parameter parameter)
        {
            Assert.IsNull(parameter.GetRawValue());
            Assert.IsNull(parameter.GetValueAsString());
            Assert.Zero(parameter.Priority);
        }

        [Test]
        public void EmptyParameterShouldHaveNoValueAndZeroPriorityAfterParsing([ValueSource(nameof(EmptyParameters))] Parameter parameter)
        {
            parameter.ValidateAndParseRawValue();

            Assert.IsNull(parameter.GetRawValue());
            Assert.IsNull(parameter.GetValueAsString());
            Assert.Zero(parameter.Priority);
        }

        [Test]
        public void SettingRawValueShouldHaveAnEffect([ValueSource(nameof(EmptyParameters))] Parameter parameter)
        {
            const string expectedValue = "example value";
            const int expectedPriority = 5;
            Assert.IsNull(parameter.GetRawValue());

            parameter.SetRawValue(expectedValue, expectedPriority);

            Assert.AreEqual(expectedValue, parameter.GetRawValue());
            Assert.AreEqual(expectedPriority, parameter.Priority);
        }

        [Test]
        public void ParsingExceptionsShouldBePropagatedCorrectly()
        {
            var expectedException = new FormatException("dummy message");
            var parameter = new ValueParameter<DateTime>(parse: _ => throw expectedException);
            parameter.SetRawValue("1994-02-27");

            var exc = Assert.Throws<FormatException>(() => parameter.ValidateAndParseRawValue());
            Assert.AreSame(expectedException, exc);
        }

        [Test]
        public void ParameterWithRawValueMatchingRegexShouldPassValidation()
        {
            var expectedValue = new DateTime();
            var parameter = new ValueParameter<DateTime>(validationRegex: new Regex(@"\d{4}-\d{2}-\d{2}"), parse: _ => expectedValue);
            parameter.SetRawValue("1994-02-27");

            parameter.ValidateAndParseRawValue();
            Assert.AreEqual(expectedValue, parameter.GetValue());
        }

        [Test]
        public void ParameterWithRawValueNotMatchingRegexShouldNotPassValidation()
        {
            var parameter = new ValueParameter<DateTime>(validationRegex: new Regex(@"^\d{4}-\d{2}-\d{2}$"), parse: _ => throw new NotImplementedException());
            parameter.SetRawValue("09:23");

            var exc = Assert.Throws<ArgumentException>(() => parameter.ValidateAndParseRawValue());
            Assert.True(exc.Message.ToLowerInvariant().Contains("validation"));
        }

        [Test]
        public void ParameterShouldHaveNonNullValueAfterParsing([ValueSource(nameof(EmptyParameters))] Parameter parameter)
        {
            const string expectedValue = "example value";
            const int expectedPriority = 5;
            Assert.IsNull(parameter.GetRawValue());

            parameter.SetRawValue(expectedValue, expectedPriority);
            parameter.ValidateAndParseRawValue();

            Assert.NotNull(parameter.GetValueAsString());
            Assert.NotNull(expectedValue);
            Assert.NotZero(expectedPriority);
        }

        [Test]
        public void ResettingParameterShouldClearValueAndPriority([ValueSource(nameof(EmptyParameters))] Parameter parameter)
        {
            parameter.SetRawValue("example value", 5);
            parameter.ValidateAndParseRawValue();

            parameter.Reset();

            Assert.IsNull(parameter.GetRawValue());
            Assert.IsNull(parameter.GetValueAsString());
            Assert.Zero(parameter.Priority);
        }

        public delegate (object, string) PrimitiveParsingDelegate(string rawValue);
        private static (object, string) ParseValueParameter<T>(string rawValue)
            where T : struct
        {
            var parameter = new ValueParameter<T>();
            parameter.SetRawValue(rawValue);
            parameter.ValidateAndParseRawValue();
            return (parameter.GetValue(), parameter.GetValueAsString());
        }
        private static (object, string) ParseParameter<T>(string rawValue)
            where T : class
        {
            var parameter = new Parameter<T>();
            parameter.SetRawValue(rawValue);
            parameter.ValidateAndParseRawValue();
            return (parameter.GetValue(), parameter.GetValueAsString());
        }

        public record PrimitiveParsingTestCase(PrimitiveParsingDelegate Parse, string RawValue, object ExpectedValue, string ExpectedValueAsString);
        private static List<PrimitiveParsingTestCase> PrimitiveParsingTestCases => new()
        {
            new PrimitiveParsingTestCase(ParseValueParameter<bool>, "true", true, "True"),
            new PrimitiveParsingTestCase(ParseValueParameter<bool>, "false", false, "False"),
            new PrimitiveParsingTestCase(ParseValueParameter<int>, "-5", -5, "-5"),
            new PrimitiveParsingTestCase(ParseValueParameter<int>, "9", 9, "9"),
            new PrimitiveParsingTestCase(ParseValueParameter<float>, "0.5", 0.5f, "0.5"),
            new PrimitiveParsingTestCase(ParseValueParameter<float>, "-2e10", -2e10f, "-2E+10"),
            new PrimitiveParsingTestCase(ParseValueParameter<char>, "9", '9', "9"),
            new PrimitiveParsingTestCase(ParseParameter<string>, "false", "false", "false"),
            new PrimitiveParsingTestCase(ParseParameter<string>, "", "", ""),
        };

        [Test]
        public void PrimitiveTypesShouldBeParsedCorrectly([ValueSource(nameof(PrimitiveParsingTestCases))] PrimitiveParsingTestCase testCase)
        {
            var (value, valueAsString) = testCase.Parse(testCase.RawValue);

            Assert.AreEqual(testCase.ExpectedValue, value);
            Assert.AreEqual(testCase.ExpectedValueAsString, valueAsString);
        }

        public record InvalidPrimitiveParsingTestCase(PrimitiveParsingDelegate Parse, string RawValue, Type ExpectedExceptionType);
        private static List<InvalidPrimitiveParsingTestCase> InvalidPrimitiveParsingTestCases => new()
        {
            new InvalidPrimitiveParsingTestCase(ParseValueParameter<bool>, "_", typeof(FormatException)),
            new InvalidPrimitiveParsingTestCase(ParseValueParameter<bool>, "5", typeof(FormatException)),
            new InvalidPrimitiveParsingTestCase(ParseValueParameter<int>, "abc", typeof(FormatException)),
            new InvalidPrimitiveParsingTestCase(ParseValueParameter<int>, "_", typeof(FormatException)),
            new InvalidPrimitiveParsingTestCase(ParseValueParameter<uint>, "-1", typeof(OverflowException)),
            new InvalidPrimitiveParsingTestCase(ParseValueParameter<uint>, "1.05", typeof(FormatException)),
            new InvalidPrimitiveParsingTestCase(ParseValueParameter<char>, "95", typeof(FormatException)),
        };

        [Test]
        public void PrimitiveParsingErrorsShouldBePropagatedCorrectly([ValueSource(nameof(InvalidPrimitiveParsingTestCases))] InvalidPrimitiveParsingTestCase testCase) =>
            _ = Assert.Throws(testCase.ExpectedExceptionType, () => testCase.Parse(testCase.RawValue));
    }
}
