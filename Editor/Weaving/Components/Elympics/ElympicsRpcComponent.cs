using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elympics.Editor.Weaving.Extensions;
using Elympics.Weaving;
using Mono.Cecil;
using Mono.Cecil.Cil;

#nullable enable

namespace Elympics.Editor.Weaving.Components.Elympics
{
    /// <summary>
    /// Class responsible for injecting IL code needed for running RPC methods.
    /// </summary>
    /// <remarks>
    /// Attention is required when referencing core library types!
    /// It is important to use <see cref="WeaverComponent.TypeSystem"/>
    /// either directly accessing its types (e.g. <see cref="TypeSystem.String"/>, <see cref="TypeSystem.Void"/>)
    /// or using it as the metadata scope in <see cref="TypeReference"/> constructor.
    /// This avoids binding to host editor CLR (System.Private.CoreLib 5.0) and prevents issues
    /// with resolving libraries when only Unity's Mono runtime (mscorlib) is available.
    /// </remarks>
    internal class ElympicsRpcComponent : WeaverComponent
    {
        private const string StartMarker = nameof(ElympicsRpcComponent) + " Start Marker";
        private const string EndMarker = nameof(ElympicsRpcComponent) + " End Marker";

        public override DefinitionType AffectedDefinitions => DefinitionType.Method;

        internal void ValidateRpcMethodDefinition(MethodDefinition methodDefinition)
        {
            if (TypeSystem is null)
                throw new InvalidOperationException($"Assembly visiting has not been started for {nameof(ElympicsRpcComponent)}");

            var typeOwner = methodDefinition.DeclaringType;
            if (typeOwner == null || !typeOwner.IsSubclassOf<ElympicsMonoBehaviour>())
                throw InvalidRpcMethodDefinitionException.NotElympicsSubclass(methodDefinition.FullName);
            if (methodDefinition.IsStatic)
                throw InvalidRpcMethodDefinitionException.Static(methodDefinition.FullName);
            if (methodDefinition.IsVirtual || methodDefinition.IsAbstract)
                throw InvalidRpcMethodDefinitionException.Virtual(methodDefinition.FullName);
            if (methodDefinition.ReturnType != TypeSystem.Void)
                throw InvalidRpcMethodDefinitionException.NonVoidReturn(methodDefinition.FullName);
            if (methodDefinition.ContainsGenericParameter)
                throw InvalidRpcMethodDefinitionException.Generic(methodDefinition.FullName);

            if (typeOwner.Methods.Count(m => m.Name == methodDefinition.Name) > 1)
                throw InvalidRpcMethodDefinitionException.Overloaded(methodDefinition.FullName);

            var unacceptableParameters = methodDefinition.Parameters
                .Select((p, i) => (Index: i, Parameter: p))
                .Where(tuple => !tuple.Parameter.ParameterType.IsPrimitive)
                .Where(tuple => tuple.Parameter.ParameterType != TypeSystem.String)
                .ToList();
            var exceptionList = new List<InvalidRpcMethodDefinitionException>();
            ParameterDefinition? metadataParameter = null;
            for (var i = 0; i < unacceptableParameters.Count; i++)
            {
                var parameter = unacceptableParameters[i].Parameter;
                if (parameter.ParameterType.FullName != typeof(RpcMetadata).FullName)
                {
                    exceptionList.Add(new UnsupportedParameterTypeException(methodDefinition.FullName, parameter.Index, parameter.Name, parameter.ParameterType.FullName));
                    continue;
                }
                if (parameter is { IsOptional: false, HasDefault: false })
                {
                    exceptionList.Add(InvalidRpcMetadataParameterDefinitionException.FromNonOptional(methodDefinition.FullName, parameter.Index, parameter.Name));
                    continue;
                }
                if (metadataParameter is null)
                {
                    metadataParameter = parameter;
                    continue;
                }
                exceptionList.Add(InvalidRpcMetadataParameterDefinitionException.FromDuplicated(methodDefinition.FullName, parameter.Index, parameter.Name, metadataParameter.Index, metadataParameter.Name));
            }

            if (exceptionList.Any())
                throw new AggregateException(exceptionList);
        }

        public override void VisitMethod(MethodDefinition methodDefinition)
        {
            if (Assembly is null || Module is null || TypeSystem is null)
                throw new InvalidOperationException($"Assembly visiting has not been started for {nameof(ElympicsRpcComponent)}");

            if (methodDefinition.GetCustomAttribute<ElympicsRpcAttribute>() == null)
                return;

            ValidateRpcMethodDefinition(methodDefinition);

            var parameters = methodDefinition.Parameters;
            var methodBody = methodDefinition.Body;
            var ilProcessor = methodBody.GetILProcessor();

            var elympicsMonoBehaviour = new ElympicsWeaverType(Assembly, typeof(ElympicsMonoBehaviour));
            var getMethodInfoMethodReference = elympicsMonoBehaviour.GetMethod(nameof(ElympicsMonoBehaviour.GetMethodInfo));
            var getRpcPropertiesMethodReference = elympicsMonoBehaviour.GetMethod(nameof(ElympicsMonoBehaviour.GetRpcProperties));
            var getElympicsBehaviourMethodReference = elympicsMonoBehaviour.GetPropertyGetter(nameof(ElympicsMonoBehaviour.ElympicsBehaviour));

            var elympicsBehaviour = new ElympicsWeaverType(Assembly, typeof(ElympicsBehaviour));
            var shouldRpcBeCapturedMethodReference = elympicsBehaviour.GetMethod(nameof(ElympicsBehaviour.ShouldRpcBeCaptured));
            var onRpcCapturedMethodReference = elympicsBehaviour.GetMethod(nameof(ElympicsBehaviour.OnRpcCaptured));
            var shouldRpcBeInvokedMethodReference = elympicsBehaviour.GetMethod(nameof(ElympicsBehaviour.ShouldRpcBeInvokedInstantly));

            var methodInfoTypeRef = new TypeReference(typeof(MethodInfo).Namespace, nameof(MethodInfo), Module, TypeSystem.CoreLibrary);
            var methodInfoVariable = new VariableDefinition(methodInfoTypeRef);
            var elympicsRpcProperties = new ElympicsWeaverType(Assembly, typeof(ElympicsRpcProperties));
            var rpcPropertiesVariable = new VariableDefinition(elympicsRpcProperties.Reference);
            methodBody.Variables.Add(methodInfoVariable);
            methodBody.Variables.Add(rpcPropertiesVariable);

            var createArrayWithMethodArguments = new List<Instruction>
            {
                ilProcessor.Create(OpCodes.Ldc_I4, parameters.Count),
                ilProcessor.Create(OpCodes.Newarr, TypeSystem.Object),
            };
            for (var i = 0; i < parameters.Count; i++)
            {
                createArrayWithMethodArguments.Add(ilProcessor.Create(OpCodes.Dup));
                createArrayWithMethodArguments.Add(ilProcessor.Create(OpCodes.Ldc_I4, i));
                createArrayWithMethodArguments.Add(ilProcessor.Create(OpCodes.Ldarg, i + 1));
                var parameter = parameters[i];
                if (parameter.ParameterType.IsValueType)
                    createArrayWithMethodArguments.Add(ilProcessor.Create(OpCodes.Box, parameter.ParameterType));
                createArrayWithMethodArguments.Add(ilProcessor.Create(OpCodes.Stelem_Ref));
            }

            var originalBodyStart = methodDefinition.Body.Instructions[0];
            var returnBeforeOriginalBody = ilProcessor.Create(OpCodes.Ret);

            // Mark the start of the injected IL code
            ilProcessor.InsertBefore(originalBodyStart, PushString(StartMarker));
            ilProcessor.InsertBefore(originalBodyStart, Pop());

            // Get MethodInfo and ElympicsRpcProperties
            ilProcessor.InsertBefore(originalBodyStart, PushThis());
            ilProcessor.InsertBefore(originalBodyStart, PushString(methodDefinition.DeclaringType.FullName));
            ilProcessor.InsertBefore(originalBodyStart, PushString(methodDefinition.Name));
            ilProcessor.InsertBefore(originalBodyStart, Call(getMethodInfoMethodReference));
            ilProcessor.InsertBefore(originalBodyStart, PopToVariable(methodInfoVariable));
            ilProcessor.InsertBefore(originalBodyStart, PushThis());
            ilProcessor.InsertBefore(originalBodyStart, PushFromVariable(methodInfoVariable));
            ilProcessor.InsertBefore(originalBodyStart, Call(getRpcPropertiesMethodReference));
            ilProcessor.InsertBefore(originalBodyStart, PopToVariable(rpcPropertiesVariable));

            // Call ShouldRpcBeInvokedInstantly and branch
            ilProcessor.InsertBefore(originalBodyStart, PushThis());
            ilProcessor.InsertBefore(originalBodyStart, Call(getElympicsBehaviourMethodReference));
            ilProcessor.InsertBefore(originalBodyStart, PushFromVariable(rpcPropertiesVariable));
            ilProcessor.InsertBefore(originalBodyStart, PushFromVariable(methodInfoVariable));
            ilProcessor.InsertBefore(originalBodyStart, Call(shouldRpcBeInvokedMethodReference));
            ilProcessor.InsertBefore(originalBodyStart, BranchIf(true, originalBodyStart));

            // Call ShouldRpcBeCaptured and branch
            ilProcessor.InsertBefore(originalBodyStart, PushThis());
            ilProcessor.InsertBefore(originalBodyStart, Call(getElympicsBehaviourMethodReference));
            ilProcessor.InsertBefore(originalBodyStart, PushFromVariable(rpcPropertiesVariable));
            ilProcessor.InsertBefore(originalBodyStart, PushFromVariable(methodInfoVariable));
            ilProcessor.InsertBefore(originalBodyStart, Call(shouldRpcBeCapturedMethodReference));
            ilProcessor.InsertBefore(originalBodyStart, BranchIf(false, returnBeforeOriginalBody));

            // Call OnRpcCaptured
            ilProcessor.InsertBefore(originalBodyStart, PushThis());
            ilProcessor.InsertBefore(originalBodyStart, Call(getElympicsBehaviourMethodReference));
            ilProcessor.InsertBefore(originalBodyStart, PushFromVariable(rpcPropertiesVariable));
            ilProcessor.InsertBefore(originalBodyStart, PushFromVariable(methodInfoVariable));
            ilProcessor.InsertBefore(originalBodyStart, PushThis());
            foreach (var instruction in createArrayWithMethodArguments)
                ilProcessor.InsertBefore(originalBodyStart, instruction);
            ilProcessor.InsertBefore(originalBodyStart, Call(onRpcCapturedMethodReference));

            // Return just before the original code
            ilProcessor.InsertBefore(originalBodyStart, returnBeforeOriginalBody);

            // Mark the end of the injected IL code
            ilProcessor.InsertBefore(originalBodyStart, PushString(EndMarker));
            ilProcessor.InsertBefore(originalBodyStart, Pop());

            // The original code continues from here (if branched to originalBodyStart)

            Instruction PopToVariable(VariableDefinition variable) => ilProcessor.Create(OpCodes.Stloc, variable);
            Instruction PushFromVariable(VariableDefinition variable) => ilProcessor.Create(OpCodes.Ldloc, variable);
            Instruction PushThis() => ilProcessor.Create(OpCodes.Ldarg_0);
            Instruction PushString(string value) => ilProcessor.Create(OpCodes.Ldstr, value);
            Instruction Pop() => ilProcessor.Create(OpCodes.Pop);
            Instruction Call(MethodReference methodReference) => ilProcessor.Create(OpCodes.Call, methodReference);
            Instruction BranchIf(bool result, Instruction instruction) => ilProcessor.Create(result ? OpCodes.Brtrue : OpCodes.Brfalse, instruction);
        }

        protected override void FinishVisiting(ModuleDefinition moduleDefinition)
        {
            if (TypeSystem is null)
                throw new InvalidOperationException($"Assembly visiting has not been started for {nameof(ElympicsRpcComponent)}");

            var elympicsVersion = ElympicsVersionRetriever.GetVersionStringFromAssembly();
            var attributeReference = moduleDefinition.ImportReference(typeof(ProcessedByElympicsAttribute));
            var attributeConstructor = new MethodReference(".ctor", moduleDefinition.TypeSystem.Void, attributeReference) { HasThis = true };
            attributeConstructor.Parameters.Add(new ParameterDefinition(moduleDefinition.TypeSystem.String));
            var attributeWithParameters = new CustomAttribute(attributeConstructor);
            attributeWithParameters.ConstructorArguments.Add(new CustomAttributeArgument(TypeSystem.String, elympicsVersion));
            moduleDefinition.Assembly.CustomAttributes.Add(attributeWithParameters);
        }
    }
}
