using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

#nullable enable

namespace Elympics
{
    internal class RpcMethodsContainer
    {
        public const BindingFlags MethodFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private readonly Dictionary<ushort, RpcMethod> _rpcMethods = new();
        private readonly Dictionary<ushort, RpcMethodDetails> _rpcMethodDetails = new();
        private readonly Dictionary<RpcMethod, ushort> _rpcMethodIds = new();

        private ushort _methodIdCounter;

        public (RpcMethod Method, RpcMethodDetails Details) this[ushort methodId] =>
            (_rpcMethods[methodId], _rpcMethodDetails[methodId]);

        public void CollectFrom(IObservable observable)
        {
            var type = observable.GetType();
            // RPC method declaring type has to be a subclass of ElympicsMonoBehaviour
            if (!type.IsSubclassOf(typeof(ElympicsMonoBehaviour)))
                return;
            var sortedTypeMethods = new List<MethodInfo>();
            while (type is not null)
            {
                // RPC method cannot be static
                var methods = type.GetMethods(MethodFlags);
                var filteredMethods = methods
                    .Where(m => m.CustomAttributes.Any(a => a.AttributeType == typeof(ElympicsRpcAttribute)))
                    .Where(m => IsValid(m, methods));
                sortedTypeMethods.AddRange(filteredMethods.OrderBy(x => x.Name));
                type = type.BaseType;
            }
            foreach (var method in sortedTypeMethods)
            {
                ElympicsLogger.LogDebug($"Registering RPC method: {method.GetFullName()} on object of type {observable.GetType().FullName} (name: {((ElympicsMonoBehaviour)observable).name})");
                var rpcMethod = new RpcMethod(method, observable);
                var rpcMethodDetails = new RpcMethodDetails(rpcMethod);
                _rpcMethods.Add(_methodIdCounter, rpcMethod);
                _rpcMethodDetails.Add(_methodIdCounter, rpcMethodDetails);
                _rpcMethodIds.Add(rpcMethod, _methodIdCounter);
                _methodIdCounter++;
            }
        }

        private static bool IsValid(MethodInfo method, MethodInfo[] typeMethods)
        {
            if (method.ReturnType != typeof(void))
            {
                ElympicsLogger.LogException(InvalidRpcMethodDefinitionException.NonVoidReturn(method.GetFullName()));
                return false;
            }
            if (method.IsAbstract || method.IsVirtual)
            {
                ElympicsLogger.LogException(InvalidRpcMethodDefinitionException.Virtual(method.GetFullName()));
                return false;
            }
            if (method.ContainsGenericParameters)
            {
                ElympicsLogger.LogException(InvalidRpcMethodDefinitionException.Generic(method.GetFullName()));
                return false;
            }
            if (typeMethods.Count(m => m.Name == method.Name) > 1)
            {
                ElympicsLogger.LogException(InvalidRpcMethodDefinitionException.Overloaded(method.GetFullName()));
                return false;
            }
            var unacceptableParameters = method.GetParameters()
                .Select((p, i) => (Index: i, Parameter: p))
                .Where(tuple => !tuple.Parameter.ParameterType.IsPrimitive)
                .Where(tuple => tuple.Parameter.ParameterType != typeof(string))
                .ToList();
            ParameterInfo? metadataParameter = null;
            for (var i = 0; i < unacceptableParameters.Count; i++)
            {
                var parameter = unacceptableParameters[i].Parameter;
                if (parameter.ParameterType.FullName != typeof(RpcMetadata).FullName)
                {
                    ElympicsLogger.LogException(new UnsupportedParameterTypeException(method.GetFullName(), parameter.Position, parameter.Name, parameter.ParameterType.FullName));
                    continue;
                }
                if (parameter is { IsOptional: false, HasDefaultValue: false })
                {
                    ElympicsLogger.LogException(InvalidRpcMetadataParameterDefinitionException.FromNonOptional(method.GetFullName(), parameter.Position, parameter.Name));
                    continue;
                }
                if (metadataParameter is null)
                {
                    metadataParameter = parameter;
                    continue;
                }
                ElympicsLogger.LogException(InvalidRpcMetadataParameterDefinitionException.FromDuplicated(method.GetFullName(), parameter.Position, parameter.Name, metadataParameter.Position, metadataParameter.Name));
            }
            return true;
        }

        public ushort GetIdOf(RpcMethod rpcMethod) => _rpcMethodIds[rpcMethod];

        internal void Clear()
        {
            _rpcMethods.Clear();
            _rpcMethodIds.Clear();
        }
    }
}
