using System;
using System.Reflection;
using Elympics.AssemblyCommunicator;
using NUnit.Framework;

namespace Elympics.Tests.AssemblyCommunicator
{
    public class CrossAssemblyEventBroadcasterTests
    {
        #region Test types

        private class InvalidArgument1 { }
        private struct InvalidArgument2 { }
        private class GenericObserver<T> : IElympicsObserver<T> { public void OnEvent(T argument) { } }
        [ElympicsEvent("Elympics.Tests.AssemblyCommunicator")]
        private class InvalidArgumentForAssembly1 { }
        [ElympicsEvent("Elympics.Tests.AssemblyCommunicator")]
        private struct InvalidArgumentForAssembly2 { }
        [ElympicsEvent("Elympics")]
        private class ValidArgument1 { }
        [ElympicsEvent("Elympics")]
        private struct ValidArgument2 { }

        #endregion

        [Test]
        public void ValidateObservers() => CrossAssemblyEventBroadcaster.ValidateObservers();

        [TestCase(typeof(InvalidArgument1), ExpectedResult = false)]
        [TestCase(typeof(InvalidArgument2), ExpectedResult = false)]
        [TestCase(typeof(InvalidArgumentForAssembly1), ExpectedResult = true)]
        [TestCase(typeof(InvalidArgumentForAssembly2), ExpectedResult = true)]
        [TestCase(typeof(ValidArgument1), ExpectedResult = true)]
        [TestCase(typeof(ValidArgument2), ExpectedResult = true)]
        public bool IsValidEventArgumentType(Type type) => CrossAssemblyEventBroadcaster.IsValidEventArgumentType(type);


        [TestCase(typeof(InvalidArgumentForAssembly1), ExpectedResult = false)]
        [TestCase(typeof(InvalidArgumentForAssembly2), ExpectedResult = false)]
        [TestCase(typeof(ValidArgument1), ExpectedResult = true)]
        [TestCase(typeof(ValidArgument2), ExpectedResult = true)]
        public bool IsValidEventArgumentTypeForObserverInAssembly(Type type) => CrossAssemblyEventBroadcaster.IsValidEventArgumentTypeForObserverInAssembly(Assembly.GetExecutingAssembly(), type);
    }
}
