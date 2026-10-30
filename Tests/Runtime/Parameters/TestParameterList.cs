using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Elympics.Tests
{
    [Category("Parameters")]
    [TestFixture]
    internal class TestParameterList
    {
        private static List<Parameter[]> parametersWithoutDuplicates = new()
        {
            Array.Empty<Parameter>(),
            new Parameter[] { new FakeParameter() },
            new Parameter[] { new FakeParameter(), new FakeParameter(), new FakeParameter() },
            new Parameter[]
            {
                new FakeParameter(envVarName: "s"),
                new FakeParameter(queryKey: "s"),
                new FakeParameter(longArgName: "s"),
                new FakeParameter(shortArgName: 's'),
                new FakeParameter(argIndex: 19),
            },
        };

        [Test]
        public void ThereShouldBeNoDuplicatesDetectedInParameterListWithoutDuplicatedKeys([ValueSource(nameof(parametersWithoutDuplicates))] Parameter[] parameters)
        {
            ParameterList.EnsureThereAreNoDuplicatedParameterKeys(parameters, parameters.ToDictionary(x => x, _ => ""));
        }

        private static List<Parameter[]> parametersWithDuplicates = new()
        {
            new Parameter[]
            {
                new FakeParameter(envVarName: "example"),
                new FakeParameter(envVarName: "example"),
            },
            new Parameter[]
            {
                new FakeParameter(queryKey: "example"),
                new FakeParameter(queryKey: "example"),
            },
            new Parameter[]
            {
                new FakeParameter(longArgName: "example"),
                new FakeParameter(longArgName: "example"),
            },
            new Parameter[]
            {
                new FakeParameter(shortArgName: 'e'),
                new FakeParameter(shortArgName: 'e'),
            },
            new Parameter[]
            {
                new FakeParameter(argIndex: 4),
                new FakeParameter(argIndex: 4),
            },
        };

        [Test]
        public void DuplicatesShouldBeDetectedInParameterListWithDuplicatedKeys([ValueSource(nameof(parametersWithDuplicates))] Parameter[] parameters)
        {
            LogAssert.Expect(LogType.Error, new Regex("(?i)duplicate"));  // LogAssert ignores RegexOptions
            var exc = Assert.Throws<ElympicsException>(() =>
                ParameterList.EnsureThereAreNoDuplicatedParameterKeys(parameters, parameters.ToDictionary(x => x, _ => "")));
            Assert.True(exc.Message.ToLowerInvariant().Contains("duplicate"));
        }

        [Test]
        public void EnvironmentVariablesShouldBeReadCorrectly()
        {
            const string envKey = "EXAMPLE_KEY1";
            const string expectedValue = "EXAMPLE_VALUE1";

            var environmentVariables = new Dictionary<string, string>
            {
                { envKey, expectedValue },
                { "EXAMPLE_KEY2", "EXAMPLE_VALUE2" },
            };

            var parameter = new FakeParameter(envVarName: envKey);
            var parametersNotExpectingEnvVariables = new List<Parameter>
            {
                new FakeParameter(),
                new FakeParameter(longArgName: "missing-arg1"),
            };
            var parametersExpectingNonPresentEnvVariables = new List<Parameter>
            {
                new FakeParameter(envVarName: "MISSING_KEY1"),
                new FakeParameter(envVarName: "MISSING_KEY2"),
            };

            var allParameters = parametersNotExpectingEnvVariables.Append(parameter)
                .Concat(parametersExpectingNonPresentEnvVariables)
                .ToArray();

            ParameterList.CollectFromEnvironmentVariables(allParameters, environmentVariables);

            Assert.AreEqual(expectedValue, parameter.GetRawValue());
            foreach (var p in parametersNotExpectingEnvVariables)
                Assert.Null(p.GetRawValue());
            foreach (var p in parametersExpectingNonPresentEnvVariables)
                Assert.Null(p.GetRawValue());
        }

        [Test]
        public void UrlQueryShouldBeReadCorrectly()
        {
            const string queryKey = "exampleKey1";
            const string expectedValue = "exampleValue1";

            var urlQuery = new NameValueCollection
            {
                { queryKey, expectedValue },
                { "exampleKey2", "exampleValue2" },
            };

            var parameter = new FakeParameter(queryKey: queryKey);
            var parametersNotExpectingQuery = new List<Parameter>
            {
                new FakeParameter(),
                new FakeParameter(longArgName: "missing-arg1"),
            };
            var parametersExpectingNonPresentQuery = new List<Parameter>
            {
                new FakeParameter(queryKey: "missingKey1"),
                new FakeParameter(queryKey: "missingKey2"),
            };

            var allParameters = parametersNotExpectingQuery.Append(parameter)
                .Concat(parametersExpectingNonPresentQuery)
                .ToArray();

            ParameterList.CollectFromUrlQuery(allParameters, urlQuery);

            Assert.AreEqual(expectedValue, parameter.GetRawValue());
            foreach (var p in parametersNotExpectingQuery)
                Assert.Null(p.GetRawValue());
            foreach (var p in parametersExpectingNonPresentQuery)
                Assert.Null(p.GetRawValue());
        }

        [Theory]
        [TestCase("example-key1", "example-value1", true)]
        [TestCase("example-key1", "example-value1", false)]
        public void LongCommandLineArgumentsShouldBeReadCorrectly(string argName, string expectedValue, bool joined)
        {
            var arguments = new List<string>
            {
                "--example-key2", "example-value2", "argument1",
            };
            if (joined)
                arguments.Insert(0, $"--{argName}={expectedValue}");
            else
            {
                arguments.Insert(0, expectedValue);
                arguments.Insert(0, $"--{argName}");
            }

            var parameter = new FakeParameter(longArgName: argName);
            var parametersNotExpectingLongArgs = new List<Parameter>
            {
                new FakeParameter(),
                new FakeParameter(queryKey: "missing-key1"),
            };
            var parametersExpectingNonPresentLongArgs = new List<Parameter>
            {
                new FakeParameter(queryKey: "missing-key1"),
                new FakeParameter(queryKey: "missing-key2"),
            };

            var allParameters = parametersNotExpectingLongArgs.Append(parameter)
                .Concat(parametersExpectingNonPresentLongArgs)
                .ToArray();

            ParameterList.CollectFromNamedArguments(allParameters, arguments);

            Assert.AreEqual(expectedValue, parameter.GetRawValue());
            foreach (var p in parametersNotExpectingLongArgs)
                Assert.Null(p.GetRawValue());
            foreach (var p in parametersExpectingNonPresentLongArgs)
                Assert.Null(p.GetRawValue());
        }

        [Test]
        public void ShortCommandLineArgumentsShouldBeReadCorrectly()
        {
            const char argName = 'k';
            const string expectedValue = "example-value1";

            var arguments = new List<string>
            {
                $"-{argName}", expectedValue, "-e", "example-value2", "argument1",
            };

            var parameter = new FakeParameter(shortArgName: argName);
            var parametersNotExpectingLongArgs = new List<Parameter>
            {
                new FakeParameter(),
                new FakeParameter(queryKey: "missing-key1"),
            };
            var parametersExpectingNonPresentLongArgs = new List<Parameter>
            {
                new FakeParameter(queryKey: "missing-key1"),
                new FakeParameter(queryKey: "missing-key2"),
            };

            var allParameters = parametersNotExpectingLongArgs.Append(parameter)
                .Concat(parametersExpectingNonPresentLongArgs)
                .ToArray();

            ParameterList.CollectFromNamedArguments(allParameters, arguments);

            Assert.AreEqual(expectedValue, parameter.GetRawValue());
            foreach (var p in parametersNotExpectingLongArgs)
                Assert.Null(p.GetRawValue());
            foreach (var p in parametersExpectingNonPresentLongArgs)
                Assert.Null(p.GetRawValue());
        }

        [Test]
        public void PositionalCommandLineArgumentsShouldBeReadCorrectly()
        {
            const int argIndex = 3;
            const string expectedValue = "example-value3";

            var arguments = new List<string>
            {
                "example-value0",
                "example-value1",
                "example-value2",
                expectedValue,
                "example-value4",
            };

            var parameter = new FakeParameter(argIndex: argIndex);
            var parametersNotExpectingPositionalArgs = new List<Parameter>
            {
                new FakeParameter(),
                new FakeParameter(queryKey: "missing-key1"),
            };
            var parametersExpectingNonPresentPositionalArgs = new List<Parameter>
            {
                new FakeParameter(argIndex: 5),
                new FakeParameter(argIndex: 6),
            };

            var allParameters = parametersNotExpectingPositionalArgs.Append(parameter)
                .Concat(parametersExpectingNonPresentPositionalArgs)
                .ToArray();

            ParameterList.CollectFromPositionalArguments(allParameters, arguments);

            Assert.AreEqual(expectedValue, parameter.GetRawValue());
            foreach (var p in parametersNotExpectingPositionalArgs)
                Assert.Null(p.GetRawValue());
            foreach (var p in parametersExpectingNonPresentPositionalArgs)
                Assert.Null(p.GetRawValue());
        }

        [Test]
        public void ParameterParsingResultsShouldBeLoggedCorrectly()
        {
            const string emptyParameterName = "empty parameter";
            const string expectedEmptyParameterValueInLog = "null";
            var emptyParameter = new FakeParameter();

            const string parsedParameterName = "parsed parameter";
            const string expectedParsedParameterValueInLog = "example value";
            var parsedParameter = new FakeParameter();
            parsedParameter.SetRawValue(expectedParsedParameterValueInLog, 3);
            parsedParameter.ValidateAndParseRawValue();

            var parameters = new Parameter[] { emptyParameter, parsedParameter };
            var parameterNames = new Dictionary<Parameter, string>
            {
                { emptyParameter, emptyParameterName },
                { parsedParameter, parsedParameterName },
            };

            LogAssert.Expect(LogType.Log, new Regex($"(?is){emptyParameterName}.*{expectedEmptyParameterValueInLog}"
                + $".*{parsedParameterName}.*{expectedParsedParameterValueInLog}"));  // LogAssert ignores RegexOptions

            ParameterList.LogResults(parameters, parameterNames);
        }

        [Test]
        public void ListOfValidParametersShouldBeParsedCorrectly()
        {
            const string emptyParameterName = "empty parameter";
            var emptyParameter = new FakeParameter();

            const string parsedParameterName = "parsed parameter";
            var parsedParameter = new FakeParameter();
            parsedParameter.SetRawValue("dummy value");

            var parameters = new Parameter[] { emptyParameter, parsedParameter };
            var parameterNames = new Dictionary<Parameter, string>
            {
                { emptyParameter, emptyParameterName },
                { parsedParameter, parsedParameterName },
            };

            var parsingResult = ParameterList.TryParseAllParameters(parameters, parameterNames);

            Assert.True(parsingResult);
            Assert.True(parsedParameter.Parsed);
            Assert.False(emptyParameter.Parsed);
        }

        [Test]
        public void OnlyValidParametersShouldBeParsedCorrectlyAndErrorsShouldBeReported()
        {
            const string emptyParameterName = "empty parameter";
            var emptyParameter = new FakeParameter();

            const string invalidParameterName = "invalid parameter";
            var invalidParameter = new FakeParameterThrowingWhenParsed();
            invalidParameter.SetRawValue("dummy value", 2);

            var parsedParameter = new FakeParameter();
            const string parsedParameterName = "parsed parameter";
            parsedParameter.SetRawValue("dummy value", 3);

            var parameters = new Parameter[] { emptyParameter, invalidParameter, parsedParameter };
            var parameterNames = new Dictionary<Parameter, string>
            {
                { emptyParameter, emptyParameterName },
                { invalidParameter, invalidParameterName },
                { parsedParameter, parsedParameterName },
            };

            LogAssert.Expect(LogType.Exception, new Regex(""));

            var parsingResult = ParameterList.TryParseAllParameters(parameters, parameterNames);

            Assert.False(parsingResult);
            Assert.True(parsedParameter.Parsed);
            Assert.False(emptyParameter.Parsed);
        }

        [TestCase("---k")]
        [TestCase("-kk")]
        [TestCase("--")]
        public void MalformedCommandLineArgumentsShouldBeIgnored(string argument)
        {
            var longParameter = new FakeParameter(longArgName: "k");
            var shortParameter = new FakeParameter(shortArgName: 'k');

            ParameterList.CollectFromNamedArguments(new Parameter[] { longParameter, shortParameter }, new List<string> { argument, "value" });

            Assert.Null(longParameter.GetRawValue());
            Assert.Null(shortParameter.GetRawValue());
        }

        [Test]
        public void LongCommandLineArgumentWithEmptyInlineValueShouldReadEmptyString()
        {
            var parameter = new FakeParameter(longArgName: "k");

            ParameterList.CollectFromNamedArguments(new Parameter[] { parameter }, new List<string> { "--k=", "next" });

            Assert.AreEqual("", parameter.GetRawValue());
        }

        [Test]
        public void LongCommandLineArgumentValueShouldKeepEqualsSignsAfterTheFirst()
        {
            var parameter = new FakeParameter(longArgName: "k");

            ParameterList.CollectFromNamedArguments(new Parameter[] { parameter }, new List<string> { "--k=a=b;c=d" });

            Assert.AreEqual("a=b;c=d", parameter.GetRawValue());
        }

        [TestCase("127.0.0.1", true)]
        [TestCase("localhost", false)]
        [TestCase("1.2.3", false)]
        [TestCase("1.2.3.4.5", false)]
        [TestCase("1..3.4", false)]
        public void HalfRemoteIpShouldBeValidatedAsDottedQuad(string value, bool isValid)
        {
            var parameter = new ParameterList().HalfRemoteIp;
            parameter.SetRawValue(value);

            if (isValid)
                Assert.DoesNotThrow(parameter.ValidateAndParseRawValue);
            else
                _ = Assert.Throws<ArgumentException>(parameter.ValidateAndParseRawValue);
        }

        [TestCase("WebGL", true)]
        [TestCase("none", true)]
        [TestCase("browser", false)]
        public void HostBridgeShouldAcceptOnlyKnownValuesIgnoringCase(string value, bool isValid)
        {
            var parameter = new ParameterList().HostBridge;
            parameter.SetRawValue(value);

            if (isValid)
                Assert.DoesNotThrow(parameter.ValidateAndParseRawValue);
            else
                _ = Assert.Throws<ArgumentException>(parameter.ValidateAndParseRawValue);
        }
    }
}
