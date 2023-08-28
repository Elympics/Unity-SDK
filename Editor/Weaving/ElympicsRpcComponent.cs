using System.Collections.Generic;
using System.Linq;
using Elympics.Weaver;
using Elympics.Weaver.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Elympics.Editor
{
    public class ElympicsRpcComponent : WeaverComponent
    {
        public override string ComponentName => nameof(ElympicsRpcComponent);
        public override DefinitionType EffectedDefintions => DefinitionType.Module | DefinitionType.Method;

        private ElympicsWeaverAssembly _assembly;

        public override void VisitModule(ModuleDefinition moduleDefinition) =>
            _assembly = new ElympicsWeaverAssembly(moduleDefinition.Assembly);

        internal void ValidateRpcMethodDefinition(MethodDefinition methodDefinition)
        {
            var typeOwner = methodDefinition.DeclaringType;
            if (typeOwner == null || !typeOwner.IsSubclassOf<ElympicsMonoBehaviour>())
                throw new InvalidRpcMethodDefinitionException($"Declaring type of RPC method {methodDefinition.FullName} has to be a subclass of {nameof(ElympicsMonoBehaviour)}");
            if (methodDefinition.IsStatic)
                throw new InvalidRpcMethodDefinitionException($"RPC method {methodDefinition.FullName} cannot be static");
            if (methodDefinition.IsVirtual || methodDefinition.IsAbstract)
                throw new InvalidRpcMethodDefinitionException($"RPC method {methodDefinition.FullName} cannot be virtual or abstract");
            if (methodDefinition.ReturnType != typeSystem.Void)
                throw new InvalidRpcMethodDefinitionException($"RPC method {methodDefinition.FullName} must return void");

            if (typeOwner.Methods.Count(m => m.Name == methodDefinition.Name) > 1)
                throw new InvalidRpcMethodDefinitionException($"RPC method {methodDefinition.FullName} cannot have an overload");

            var unacceptableParameters = methodDefinition.Parameters
                .Where(argument => !argument.ParameterType.IsPrimitive)
                .Where(argument => argument.ParameterType != typeSystem.String);
            foreach (var parameter in unacceptableParameters)
                throw new InvalidRpcMethodDefinitionException($"RPC method {methodDefinition.FullName} can only have "
                    + $"primitive types or strings as parameters. However parameter {parameter.Name} is of type "
                    + $"{parameter.ParameterType.FullName}");
        }

        public override void VisitMethod(MethodDefinition methodDefinition)
        {
            if (methodDefinition.GetCustomAttribute<ElympicsRpcAttribute>() == null)
                return;

            ValidateRpcMethodDefinition(methodDefinition);

            var parameters = methodDefinition.Parameters;
            var methodBody = methodDefinition.Body;
            var rpcILProcessor = methodBody.GetILProcessor();

            var getMethodInfoMethodReference = _assembly.ElympicsMonoBehaviour.GetMethod(nameof(ElympicsMonoBehaviour.GetMethodInfo));
            var getRpcPropertiesMethodReference = _assembly.ElympicsMonoBehaviour.GetMethod(nameof(ElympicsMonoBehaviour.GetRpcProperties));

            var validateRpcContextMethodReference = _assembly.ElympicsBehaviour.GetMethod(nameof(ElympicsBehaviour.ValidateRpcContext));
            var shouldBeCapturedMethodReference = _assembly.ElympicsBehaviour.GetMethod(nameof(ElympicsBehaviour.ShouldRpcBeCaptured));
            var onRpcCapturedMethodReference = _assembly.ElympicsBehaviour.GetMethod(nameof(ElympicsBehaviour.OnRpcCaptured));
            var shouldBeInvokedMethodReference = _assembly.ElympicsBehaviour.GetMethod(nameof(ElympicsBehaviour.ShouldRpcBeInvoked));

            var methodInfoVariable = new VariableDefinition(_assembly.Assembly.MainModule.ImportReference(typeof(System.Reflection.MethodInfo)));
            var rpcPropertiesVariable = new VariableDefinition(_assembly.ElympicsRpcProperties.Reference);
            var getElympicsBehaviourMethodReference = _assembly.ElympicsMonoBehaviour.GetPropertyGetter(nameof(ElympicsMonoBehaviour.ElympicsBehaviour));
            methodBody.Variables.Add(methodInfoVariable);
            methodBody.Variables.Add(rpcPropertiesVariable);

            var shouldBeInvokedBranch = rpcILProcessor.Create(OpCodes.Nop);
            var originalMethodBodyBranch = rpcILProcessor.Create(OpCodes.Nop);

            var loadThisOnStack = rpcILProcessor.Create(OpCodes.Ldarg_0);
            var returnEarly = rpcILProcessor.Create(OpCodes.Ret);
            var loadMethodNameOnStack = rpcILProcessor.Create(OpCodes.Ldstr, methodDefinition.Name);
            var callGetMethodInfo = rpcILProcessor.Create(OpCodes.Call, getMethodInfoMethodReference);
            var storeMethodInfoToVariable = rpcILProcessor.Create(OpCodes.Stloc, methodInfoVariable);
            var callGetRpcProperties = rpcILProcessor.Create(OpCodes.Call, getRpcPropertiesMethodReference);
            var storeRpcPropertiesToVariable = rpcILProcessor.Create(OpCodes.Stloc, rpcPropertiesVariable);

            var callGetElympicsBehaviour = rpcILProcessor.Create(OpCodes.Call, getElympicsBehaviourMethodReference);
            var loadMethodInfoFromVariable = rpcILProcessor.Create(OpCodes.Ldloc, methodInfoVariable);
            var loadRpcPropertiesFromVariable = rpcILProcessor.Create(OpCodes.Ldloc, rpcPropertiesVariable);
            var callValidateRpcContext = rpcILProcessor.Create(OpCodes.Call, validateRpcContextMethodReference);
            var callShouldBeCaptured = rpcILProcessor.Create(OpCodes.Call, shouldBeCapturedMethodReference);
            var branchShouldBeCapturedMethod = rpcILProcessor.Create(OpCodes.Brfalse, shouldBeInvokedBranch);
            var callOnRpcCaptured = rpcILProcessor.Create(OpCodes.Call, onRpcCapturedMethodReference);
            var callShouldBeInvoked = rpcILProcessor.Create(OpCodes.Call, shouldBeInvokedMethodReference);
            var branchOriginalMethodBody = rpcILProcessor.Create(OpCodes.Brtrue, originalMethodBodyBranch);

            var createNewArrayContainingRpcArguments = new List<Instruction>
            {
                rpcILProcessor.Create(OpCodes.Ldc_I4, parameters.Count),
                rpcILProcessor.Create(OpCodes.Newarr, _assembly.Assembly.MainModule.TypeSystem.Object),
            };
            for (var i = 0; i < parameters.Count; i++)
            {
                createNewArrayContainingRpcArguments.Add(rpcILProcessor.Create(OpCodes.Dup));
                createNewArrayContainingRpcArguments.Add(rpcILProcessor.Create(OpCodes.Ldc_I4, i));
                createNewArrayContainingRpcArguments.Add(rpcILProcessor.Create(OpCodes.Ldarg, i + 1));
                var parameterData = parameters[i];
                if (parameterData.ParameterType.IsValueType)
                    createNewArrayContainingRpcArguments.Add(rpcILProcessor.Create(OpCodes.Box, parameterData.ParameterType));
                createNewArrayContainingRpcArguments.Add(rpcILProcessor.Create(OpCodes.Stelem_Ref));
            }

            var firstInstruction = methodDefinition.Body.Instructions[0];

            // Get MethodInfo and ElympicsRpcProperties
            rpcILProcessor.InsertBefore(firstInstruction, loadThisOnStack);
            rpcILProcessor.InsertBefore(firstInstruction, loadMethodNameOnStack);
            rpcILProcessor.InsertBefore(firstInstruction, callGetMethodInfo);
            rpcILProcessor.InsertBefore(firstInstruction, storeMethodInfoToVariable);
            rpcILProcessor.InsertBefore(firstInstruction, loadThisOnStack);
            rpcILProcessor.InsertBefore(firstInstruction, loadMethodInfoFromVariable);
            rpcILProcessor.InsertBefore(firstInstruction, callGetRpcProperties);
            rpcILProcessor.InsertBefore(firstInstruction, storeRpcPropertiesToVariable);

            // Call ValidateRpcContext
            rpcILProcessor.InsertBefore(firstInstruction, loadThisOnStack);
            rpcILProcessor.InsertBefore(firstInstruction, callGetElympicsBehaviour);
            rpcILProcessor.InsertBefore(firstInstruction, loadRpcPropertiesFromVariable);
            rpcILProcessor.InsertBefore(firstInstruction, loadMethodInfoFromVariable);
            rpcILProcessor.InsertBefore(firstInstruction, callValidateRpcContext);

            // Call ShouldBeCaptured and branch
            rpcILProcessor.InsertBefore(firstInstruction, loadThisOnStack);
            rpcILProcessor.InsertBefore(firstInstruction, callGetElympicsBehaviour);
            rpcILProcessor.InsertBefore(firstInstruction, loadRpcPropertiesFromVariable);
            rpcILProcessor.InsertBefore(firstInstruction, loadMethodInfoFromVariable);
            rpcILProcessor.InsertBefore(firstInstruction, callShouldBeCaptured);
            rpcILProcessor.InsertBefore(firstInstruction, branchShouldBeCapturedMethod);

            // Call OnRpcCaptured
            rpcILProcessor.InsertBefore(firstInstruction, loadThisOnStack);
            rpcILProcessor.InsertBefore(firstInstruction, callGetElympicsBehaviour);
            rpcILProcessor.InsertBefore(firstInstruction, loadRpcPropertiesFromVariable);
            rpcILProcessor.InsertBefore(firstInstruction, loadMethodInfoFromVariable);
            rpcILProcessor.InsertBefore(firstInstruction, loadThisOnStack);
            foreach (var instruction in createNewArrayContainingRpcArguments)
                rpcILProcessor.InsertBefore(firstInstruction, instruction);
            rpcILProcessor.InsertBefore(firstInstruction, callOnRpcCaptured);

            // Call ShouldBeInvoked and branch
            rpcILProcessor.InsertBefore(firstInstruction, shouldBeInvokedBranch);
            rpcILProcessor.InsertBefore(firstInstruction, loadThisOnStack);
            rpcILProcessor.InsertBefore(firstInstruction, callGetElympicsBehaviour);
            rpcILProcessor.InsertBefore(firstInstruction, loadRpcPropertiesFromVariable);
            rpcILProcessor.InsertBefore(firstInstruction, loadMethodInfoFromVariable);
            rpcILProcessor.InsertBefore(firstInstruction, callShouldBeInvoked);
            rpcILProcessor.InsertBefore(firstInstruction, branchOriginalMethodBody);
            rpcILProcessor.InsertBefore(firstInstruction, returnEarly);

            // Calling standard method
            rpcILProcessor.InsertBefore(firstInstruction, originalMethodBodyBranch);
        }
    }
}
