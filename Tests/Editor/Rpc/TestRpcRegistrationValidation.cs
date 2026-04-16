using System;
using System.Linq;
using System.Reflection;
using Elympics.Editor.Weaving.Components.Elympics;
using Mono.Cecil;
using NUnit.Framework;
using static Elympics.Tests.CustomAsserts;

namespace Elympics.Editor.Tests
{
    [Category("RPC")]
    public class TestRpcRegistrationValidation
    {
        private ElympicsRpcComponent _component;
        private SimpleAssemblyResolver _assemblyResolver;

        [SetUp]
        public void PrepareComponent()
        {
            _component = new ElympicsRpcComponent();
            _assemblyResolver = new SimpleAssemblyResolver();
            _assemblyResolver.AssemblyDefinitions = new[]
            {
                AssemblyDefinition.ReadAssembly(typeof(void).Assembly.Location, new ReaderParameters { AssemblyResolver = _assemblyResolver }),
                AssemblyDefinition.ReadAssembly(typeof(TestRpcRegistrationValidation).Assembly.Location, new ReaderParameters { AssemblyResolver = _assemblyResolver }),
                AssemblyDefinition.ReadAssembly(typeof(ElympicsMonoBehaviour).Assembly.Location, new ReaderParameters { AssemblyResolver = _assemblyResolver }),
            };
        }

        private void RunValidation(Type declaringType, string methodName)
        {
            var assembly = AssemblyDefinition.ReadAssembly(declaringType.Assembly.Location,
                new ReaderParameters { AssemblyResolver = _assemblyResolver });
            var methodInfo = declaringType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .First(m => m.Name == methodName);
            var methodReference = assembly.MainModule.ImportReference(methodInfo);
            var methodDefinition = methodReference.Resolve();
            _component.OnBeforeModuleEdited(methodDefinition.Module);
            _component.ValidateRpcMethodDefinition(methodDefinition);
        }

        [Test]
        [TestCase(typeof(NotSubclass), nameof(NotSubclass.Method))]
        [TestCase(typeof(NotElympicsMonoBehaviourSubclass), nameof(NotElympicsMonoBehaviourSubclass.Method))]
        public void MethodsOfTypesNotInheritingFromElympicsMonoBehaviourShouldNotPassValidation(Type declaringType, string methodName)
        {
            var exception = Assert.Throws<InvalidRpcMethodDefinitionException>(() =>
                RunValidation(declaringType, methodName));
            Assert.True(exception.Message.Contains(nameof(ElympicsMonoBehaviour)));
        }

        [Test]
        public void StaticMethodsShouldNotPassValidation()
        {
            var exception = Assert.Throws<InvalidRpcMethodDefinitionException>(() =>
                RunValidation(typeof(ElympicsMonoBehaviourSubclass), nameof(ElympicsMonoBehaviourSubclass.StaticMethod)));
            Assert.True(exception.Message.Contains("static"));
        }

        [Test]
        public void VirtualMethodsShouldNotPassValidation()
        {
            var exception = Assert.Throws<InvalidRpcMethodDefinitionException>(() =>
                RunValidation(typeof(ElympicsMonoBehaviourSubclass), nameof(ElympicsMonoBehaviourSubclass.VirtualMethod)));
            Assert.True(exception.Message.Contains("virtual"));
        }

        [Test]
        public void AbstractMethodsShouldNotPassValidation()
        {
            var exception = Assert.Throws<InvalidRpcMethodDefinitionException>(() =>
                RunValidation(typeof(ElympicsMonoBehaviourSubclass), nameof(ElympicsMonoBehaviourSubclass.AbstractMethod)));
            Assert.True(exception.Message.Contains("abstract"));
        }

        [Test]
        public void NonVoidMethodsShouldNotPassValidation()
        {
            var exception = Assert.Throws<InvalidRpcMethodDefinitionException>(() =>
                RunValidation(typeof(ElympicsMonoBehaviourSubclass), nameof(ElympicsMonoBehaviourSubclass.NonVoidMethod)));
            Assert.True(exception.Message.Contains("void"));
        }

        [Test]
        [TestCase(nameof(ElympicsMonoBehaviourSubclass.ReferenceTypeArgumentMethod))]
        [TestCase(nameof(ElympicsMonoBehaviourSubclass.NonPrimitiveValueTypeArgumentMethod))]
        [TestCase(nameof(ElympicsMonoBehaviourSubclass.InArgumentMethod))]
        [TestCase(nameof(ElympicsMonoBehaviourSubclass.RefArgumentMethod))]
        [TestCase(nameof(ElympicsMonoBehaviourSubclass.OutArgumentMethod))]
        public void MethodsWithArgumentsThatAreNotPrimitiveTypesOrStringsShouldNotPassValidation(string methodName)
        {
            var exception = AssertThrowsAggregated<InvalidRpcMethodDefinitionException>(() =>
                RunValidation(typeof(ElympicsMonoBehaviourSubclass), methodName));
            Assert.That(exception.Message, Contains.Substring("unsupported type"));
        }

        [Test]
        public void MethodsWhichHaveOverloadsShouldNotPassValidation()
        {
            var exception = Assert.Throws<InvalidRpcMethodDefinitionException>(() =>
                RunValidation(typeof(ElympicsMonoBehaviourSubclass), nameof(ElympicsMonoBehaviourSubclass.OverloadedMethod)));
            Assert.True(exception.Message.Contains("overload"));
        }

        [Test]
        public void MethodsWhichHaveMoreThanOneMetadataParameterShouldNotPassValidation()
        {
            var exception = AssertThrowsAggregated<InvalidRpcMethodDefinitionException>(() =>
                RunValidation(typeof(ElympicsMonoBehaviourSubclass), nameof(ElympicsMonoBehaviourSubclass.MoreThanOneMetadata)));
            Assert.That(exception.Message, Contains.Substring(nameof(RpcMetadata)));
            Assert.That(exception.Message, Contains.Substring("too many times"));
        }

        [Test]
        public void MethodsWhichHaveNonOptionalMetadataParameterShouldNotPassValidation()
        {
            var exception = AssertThrowsAggregated<InvalidRpcMethodDefinitionException>(() =>
                RunValidation(typeof(ElympicsMonoBehaviourSubclass), nameof(ElympicsMonoBehaviourSubclass.RequiredMetadata)));
            Assert.That(exception.Message, Contains.Substring(nameof(RpcMetadata)));
            Assert.That(exception.Message, Contains.Substring("optional"));
        }

        [Test]
        [TestCase(nameof(ElympicsMonoBehaviourSubclass.ValidMethod))]
        [TestCase(nameof(ElympicsMonoBehaviourSubclass.ValidMethodWithArguments))]
        [TestCase(nameof(ElympicsMonoBehaviourSubclass.ValidMethodWithMetadata))]
        [TestCase(nameof(ElympicsMonoBehaviourSubclass.ValidMethodWithArgumentsAndMetadata))]
        [TestCase(ElympicsMonoBehaviourSubclass.PrivateValidMethodName)]
        public void MethodsWithValidDefinitionShouldPassValidation(string methodName)
        {
            Assert.DoesNotThrow(() => RunValidation(typeof(ElympicsMonoBehaviourSubclass), methodName));
        }

#pragma warning disable IDE0060
        private abstract class ElympicsMonoBehaviourSubclass : ElympicsMonoBehaviour
        {
            public static void StaticMethod() { }
            public virtual void VirtualMethod() { }
            public abstract void AbstractMethod();
            public int NonVoidMethod() => 0;
            public void ReferenceTypeArgumentMethod(int[] intArr) { }
            public void NonPrimitiveValueTypeArgumentMethod(MyValueType valueType) { }
            public void InArgumentMethod(in byte b) { }
            public void RefArgumentMethod(ref float f) { }
            public void OutArgumentMethod(out string str) => str = "";
            public void OverloadedMethod() { }
            public void OverloadedMethod(int a) { }
            public void MoreThanOneMetadata(RpcMetadata metadata1 = default, RpcMetadata metadata2 = default) { }
            public void RequiredMetadata(RpcMetadata metadata) { }

            public void ValidMethod() { }
            public void ValidMethodWithArguments(int a, float b, string c) { }
            public void ValidMethodWithMetadata(RpcMetadata metadata = default) { }
            public void ValidMethodWithArgumentsAndMetadata(int a, float b, string c, RpcMetadata metadata = default) { }
            private void PrivateValidMethod() { }
            public const string PrivateValidMethodName = nameof(PrivateValidMethod);

            public struct MyValueType { }
        }
#pragma warning restore IDE0060

        private class NotSubclass
        {
            public void Method() { }
        }

        private class NotElympicsMonoBehaviourSubclass : NotSubclass
        {
            public new void Method() { }
        }

        // based on: https://stackoverflow.com/a/57517329
        private class SimpleAssemblyResolver : IAssemblyResolver
        {
            public AssemblyDefinition[] AssemblyDefinitions;

            public AssemblyDefinition Resolve(AssemblyNameReference name) => AssemblyDefinitions.First(x => x.Name.Name == name.Name);
            public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters) => Resolve(name);
            public void Dispose() => AssemblyDefinitions = null;
        }
    }
}
