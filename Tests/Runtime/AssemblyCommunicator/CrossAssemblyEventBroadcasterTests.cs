using System;
using System.Linq;
using System.Reflection;
using Elympics.AssemblyCommunicator;
using NUnit.Framework;

namespace Elympics.Tests.AssemblyCommunicator
{
    public class CrossAssemblyEventBroadcasterTests
    {
        #region Test types

        private class Argument1 { }
        private struct Argument2 { }

        #endregion

        [Test]
        public void ValidateObservers() => CrossAssemblyEventBroadcaster.ValidateObservers();

        [TestCase(typeof(Argument1), false, ExpectedResult = true)]
        [TestCase(typeof(Argument2), false, ExpectedResult = true)]
        [TestCase(typeof(Argument1), true, ExpectedResult = false)]
        [TestCase(typeof(Argument2), true, ExpectedResult = false)]
        public bool IsValidEventArgumentTypeForObserverInAssembly(Type type, bool thisAssembly)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var assembly = thisAssembly ? executingAssembly : AppDomain.CurrentDomain.GetAssemblies().First(a => a != executingAssembly);
            return CrossAssemblyEventBroadcaster.IsValidEventArgumentTypeForObserverInAssembly(assembly, type);
        }
    }
}
