using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Elympics.Tests.Parameters
{
    [Category("Parameters")]
    [TestFixture]
    internal class TestApplicationParameters
    {
        private Parameter[] _parameters;

        [OneTimeSetUp]
        public void ScanParameters()
        {
            _parameters = typeof(ParameterList).GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Select(x => x.GetValue(ApplicationParameters.Parameters) as Parameter)
                .Where(p => p is not null)
                .ToArray();
        }

        [Test]
        public void ParametersShouldHaveNoValueAndZeroPriorityAfterProvidingEmptyInput()
        {
            var envVariables = new Dictionary<string, string>();
            var arguments = Array.Empty<string>();
            var urlQuery = new NameValueCollection();
            var elympicsUrlQuery = new NameValueCollection();

            var initializationResult = ApplicationParameters.Parameters.Initialize(envVariables, arguments, urlQuery, elympicsUrlQuery);

            Assert.IsTrue(initializationResult);
            Assert.IsTrue(ApplicationParameters.Parameters.Initialized);
            foreach (var parameter in _parameters)
            {
                Assert.IsNull(parameter.GetRawValue());
                Assert.IsNull(parameter.GetValueAsString());
                Assert.Zero(parameter.Priority);
            }
        }

        [Test]
        public void PlayPadLobbyParametersAreReadFromEnvironmentVariables()
        {
            var envVariables = new Dictionary<string, string>
            {
                { "ELYMPICS_LOBBY", "https://lobby.example/v2/lobby" },
                { "ELYMPICS_AUTH", "https://auth.example/v2/auth" },
                { "ELYMPICS_CLIENT_SECRET", "secret" },
            };

            Assert.IsTrue(ApplicationParameters.Parameters.Initialize(envVariables, Array.Empty<string>(), new NameValueCollection(), new NameValueCollection()));

            Assert.That(ApplicationParameters.Parameters.LobbyEndpoint.GetValue(), Is.EqualTo(new Uri("https://lobby.example/v2/lobby")));
            Assert.That(ApplicationParameters.Parameters.AuthEndpoint.GetValue(), Is.EqualTo(new Uri("https://auth.example/v2/auth")));
            Assert.That(ApplicationParameters.Parameters.ClientSecret.GetValue(), Is.EqualTo("secret"));
        }

        public record OnlineModeTestCase(string[] EnvKeys, bool ShouldBeServer);
        private static List<OnlineModeTestCase> OnlineModeTestCases => new()
        {
            new OnlineModeTestCase(new[] { "ELYMPICS" }, true),
            new OnlineModeTestCase(Array.Empty<string>(), false),
        };

        [Test]
        public void ElympicsOnlineModeShouldBeChosenCorrectlyBasedOnProvidedEnvironmentVariables([ValueSource(nameof(OnlineModeTestCases))] OnlineModeTestCase testCase)
        {
            var envVariables = testCase.EnvKeys.ToDictionary(envKey => envKey, _ => "");
            var arguments = Array.Empty<string>();
            var urlQuery = new NameValueCollection();
            var elympicsUrlQuery = new NameValueCollection();

            var initializationResult = ApplicationParameters.Parameters.Initialize(envVariables, arguments, urlQuery, elympicsUrlQuery);

            Assert.IsTrue(initializationResult);
            Assert.IsTrue(ApplicationParameters.Parameters.Initialized);
            Assert.AreEqual(testCase.ShouldBeServer, ApplicationParameters.ShouldLoadElympicsOnlineServer);
        }
    }
}
