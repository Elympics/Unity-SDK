using System;
using System.Linq;
using System.Reflection;
using Elympics.Weaver;
using Mono.Cecil;
using NUnit.Framework;

namespace Elympics.Editor.Tests
{
    [Category("RPC")]
    public class TestMethodInvokedComponent
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
                AssemblyDefinition.ReadAssembly(typeof(TestMethodInvokedComponent).Assembly.Location, new ReaderParameters { AssemblyResolver = _assemblyResolver }),
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
            _component.OnBeforeModuleEdited(methodDefinition.Module, new Log(null));
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
            var exception = Assert.Throws<InvalidRpcMethodDefinitionException>(() =>
                RunValidation(typeof(ElympicsMonoBehaviourSubclass), methodName));
            Assert.True(exception.Message.Contains("primitive"));
            Assert.True(exception.Message.Contains("string"));
        }

        [Test]
        public void MethodsWhichHaveOverloadsShouldNotPassValidation()
        {
            var exception = Assert.Throws<InvalidRpcMethodDefinitionException>(() =>
                RunValidation(typeof(ElympicsMonoBehaviourSubclass), nameof(ElympicsMonoBehaviourSubclass.OverloadedMethod)));
            Assert.True(exception.Message.Contains("overload"));
        }

        [Test]
        [TestCase(nameof(ElympicsMonoBehaviourSubclass.ValidMethod))]
        [TestCase(nameof(ElympicsMonoBehaviourSubclass.ValidMethodWithArguments))]
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

            public void ValidMethod() { }
            public void ValidMethodWithArguments(int a, float b, string c) { }
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
