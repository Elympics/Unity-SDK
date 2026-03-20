using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Editor.Weaving.Extensions;
using Elympics.Weaving;
using Mono.Cecil;
using Mono.Cecil.Cil;

#nullable enable

namespace Elympics.Editor.Weaving.Components.Elympics
{
    internal class ElympicsRpcComponent : WeaverComponent
    {
        private const string StartMarker = nameof(ElympicsRpcComponent) + " Start Marker";
        private const string EndMarker = nameof(ElympicsRpcComponent) + " End Marker";

        public override DefinitionType AffectedDefinitions => DefinitionType.Method;

        private ElympicsWeaverAssembly? _assembly;

        protected override void StartVisiting(ModuleDefinition moduleDefinition) =>
            _assembly = new ElympicsWeaverAssembly(moduleDefinition.Assembly);

        internal void ValidateRpcMethodDefinition(MethodDefinition methodDefinition)
        {
            var typeOwner = methodDefinition.DeclaringType;
            if (typeOwner == null || !typeOwner.IsSubclassOf<ElympicsMonoBehaviour>())
                throw new InvalidRpcMethodDefinitionException(methodDefinition.FullName, $"RPC method declaring type has to be a subclass of {nameof(ElympicsMonoBehaviour)}");
            if (methodDefinition.IsStatic)
                throw new InvalidRpcMethodDefinitionException(methodDefinition.FullName, "RPC method cannot be static");
            if (methodDefinition.IsVirtual || methodDefinition.IsAbstract)
                throw new InvalidRpcMethodDefinitionException(methodDefinition.FullName, "RPC method cannot be virtual or abstract");
            if (methodDefinition.ReturnType != TypeSystem.Void)
                throw new InvalidRpcMethodDefinitionException(methodDefinition.FullName, "RPC method must return void");

            if (typeOwner.Methods.Count(m => m.Name == methodDefinition.Name) > 1)
                throw new InvalidRpcMethodDefinitionException(methodDefinition.FullName, "RPC method cannot have an overload");

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
            if (_assembly is null)
                throw new InvalidOperationException($"Assembly visiting has not been started for ${nameof(ElympicsRpcComponent)}");

            if (methodDefinition.GetCustomAttribute<ElympicsRpcAttribute>() == null)
                return;

            ValidateRpcMethodDefinition(methodDefinition);

            var parameters = methodDefinition.Parameters;
            var methodBody = methodDefinition.Body;
            var ilProcessor = methodBody.GetILProcessor();

            var getMethodInfoMethodReference = _assembly.ElympicsMonoBehaviour.GetMethod(nameof(ElympicsMonoBehaviour.GetMethodInfo));
            var getRpcPropertiesMethodReference = _assembly.ElympicsMonoBehaviour.GetMethod(nameof(ElympicsMonoBehaviour.GetRpcProperties));

            var shouldRpcBeCapturedMethodReference = _assembly.ElympicsBehaviour.GetMethod(nameof(ElympicsBehaviour.ShouldRpcBeCaptured));
            var onRpcCapturedMethodReference = _assembly.ElympicsBehaviour.GetMethod(nameof(ElympicsBehaviour.OnRpcCaptured));
            var shouldRpcBeInvokedMethodReference = _assembly.ElympicsBehaviour.GetMethod(nameof(ElympicsBehaviour.ShouldRpcBeInvokedInstantly));

            var methodInfoVariable = new VariableDefinition(_assembly.Assembly.MainModule.ImportReference(typeof(System.Reflection.MethodInfo)));
            var rpcPropertiesVariable = new VariableDefinition(_assembly.ElympicsRpcProperties.Reference);
            methodBody.Variables.Add(methodInfoVariable);
            methodBody.Variables.Add(rpcPropertiesVariable);

            var getElympicsBehaviourMethodReference = _assembly.ElympicsMonoBehaviour.GetPropertyGetter(nameof(ElympicsMonoBehaviour.ElympicsBehaviour));

            var loadThisOnStack = ilProcessor.Create(OpCodes.Ldarg_0);
            var loadMethodNameOnStack = ilProcessor.Create(OpCodes.Ldstr, methodDefinition.Name);
            var callGetMethodInfo = ilProcessor.Create(OpCodes.Call, getMethodInfoMethodReference);
            var storeMethodInfoToVariable = ilProcessor.Create(OpCodes.Stloc, methodInfoVariable);
            var callGetRpcProperties = ilProcessor.Create(OpCodes.Call, getRpcPropertiesMethodReference);
            var storeRpcPropertiesToVariable = ilProcessor.Create(OpCodes.Stloc, rpcPropertiesVariable);

            var callGetElympicsBehaviour = ilProcessor.Create(OpCodes.Call, getElympicsBehaviourMethodReference);
            var loadMethodInfoFromVariable = ilProcessor.Create(OpCodes.Ldloc, methodInfoVariable);
            var loadRpcPropertiesFromVariable = ilProcessor.Create(OpCodes.Ldloc, rpcPropertiesVariable);
            var callShouldBeCaptured = ilProcessor.Create(OpCodes.Call, shouldRpcBeCapturedMethodReference);
            var callOnRpcCaptured = ilProcessor.Create(OpCodes.Call, onRpcCapturedMethodReference);
            var callShouldBeInvoked = ilProcessor.Create(OpCodes.Call, shouldRpcBeInvokedMethodReference);

            var createArrayWithMethodArguments = new List<Instruction>
            {
                ilProcessor.Create(OpCodes.Ldc_I4, parameters.Count),
                ilProcessor.Create(OpCodes.Newarr, _assembly.Assembly.MainModule.TypeSystem.Object),
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

            var returnBeforeOriginalBody = ilProcessor.Create(OpCodes.Ret);
            var originalBodyStart = methodDefinition.Body.Instructions[0];

            var loadStartMarker = ilProcessor.Create(OpCodes.Ldstr, StartMarker);
            var loadEndMarker = ilProcessor.Create(OpCodes.Ldstr, EndMarker);
            var pop = ilProcessor.Create(OpCodes.Pop);

            // Mark the start of the injected IL code
            ilProcessor.InsertBefore(originalBodyStart, loadStartMarker);
            ilProcessor.InsertBefore(originalBodyStart, pop);

            // Get MethodInfo and ElympicsRpcProperties
            ilProcessor.InsertBefore(originalBodyStart, loadThisOnStack);
            ilProcessor.InsertBefore(originalBodyStart, loadMethodNameOnStack);
            ilProcessor.InsertBefore(originalBodyStart, callGetMethodInfo);
            ilProcessor.InsertBefore(originalBodyStart, storeMethodInfoToVariable);
            ilProcessor.InsertBefore(originalBodyStart, loadThisOnStack);
            ilProcessor.InsertBefore(originalBodyStart, loadMethodInfoFromVariable);
            ilProcessor.InsertBefore(originalBodyStart, callGetRpcProperties);
            ilProcessor.InsertBefore(originalBodyStart, storeRpcPropertiesToVariable);

            // Call ShouldRpcBeInvokedInstantly and branch
            ilProcessor.InsertBefore(originalBodyStart, loadThisOnStack);
            ilProcessor.InsertBefore(originalBodyStart, callGetElympicsBehaviour);
            ilProcessor.InsertBefore(originalBodyStart, loadRpcPropertiesFromVariable);
            ilProcessor.InsertBefore(originalBodyStart, loadMethodInfoFromVariable);
            ilProcessor.InsertBefore(originalBodyStart, callShouldBeInvoked);
            ilProcessor.InsertBefore(originalBodyStart, ilProcessor.Create(OpCodes.Brtrue, originalBodyStart));

            // Call ShouldRpcBeCaptured and branch
            ilProcessor.InsertBefore(originalBodyStart, loadThisOnStack);
            ilProcessor.InsertBefore(originalBodyStart, callGetElympicsBehaviour);
            ilProcessor.InsertBefore(originalBodyStart, loadRpcPropertiesFromVariable);
            ilProcessor.InsertBefore(originalBodyStart, loadMethodInfoFromVariable);
            ilProcessor.InsertBefore(originalBodyStart, callShouldBeCaptured);
            ilProcessor.InsertBefore(originalBodyStart, ilProcessor.Create(OpCodes.Brfalse, returnBeforeOriginalBody));

            // Call OnRpcCaptured
            ilProcessor.InsertBefore(originalBodyStart, loadThisOnStack);
            ilProcessor.InsertBefore(originalBodyStart, callGetElympicsBehaviour);
            ilProcessor.InsertBefore(originalBodyStart, loadRpcPropertiesFromVariable);
            ilProcessor.InsertBefore(originalBodyStart, loadMethodInfoFromVariable);
            ilProcessor.InsertBefore(originalBodyStart, loadThisOnStack);
            foreach (var instruction in createArrayWithMethodArguments)
                ilProcessor.InsertBefore(originalBodyStart, instruction);
            ilProcessor.InsertBefore(originalBodyStart, callOnRpcCaptured);

            // Return just before the original code
            ilProcessor.InsertBefore(originalBodyStart, returnBeforeOriginalBody);

            // Mark the end of the injected IL code
            ilProcessor.InsertBefore(originalBodyStart, loadEndMarker);
            ilProcessor.InsertBefore(originalBodyStart, pop);

            // The original code continues from here (if branched to originalBodyStart)
        }

        protected override void FinishVisiting(ModuleDefinition moduleDefinition)
        {
            var elympicsVersion = ElympicsVersionRetriever.GetVersionStringFromAssembly();
            var processedAttribute = new CustomAttribute(moduleDefinition
                .ImportReference(typeof(ProcessedByElympicsAttribute).GetConstructor(new[] { typeof(string) })));
            processedAttribute.ConstructorArguments.Add(new CustomAttributeArgument(TypeSystem.Boolean, elympicsVersion));
            moduleDefinition.Assembly.CustomAttributes.Add(processedAttribute);

            _assembly = null;
        }
    }
}
