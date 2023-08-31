using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Elympics
{
    internal class RpcMethodsContainer
    {
        private readonly Dictionary<ushort, RpcMethod> _rpcMethods = new();
        private readonly Dictionary<RpcMethod, ushort> _rpcMethodIds = new();

        private ushort _methodIdCounter;

        public RpcMethod this[ushort methodId] => _rpcMethods[methodId];

        public void CollectFrom(IObservable observable)
        {
            var sortedTypeMethods = observable.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .OrderBy(x => x.Name)
                .ToArray();
            foreach (var method in sortedTypeMethods)
            {
                var attributes = method.CustomAttributes;
                if (attributes.All(attribute => attribute.AttributeType != typeof(ElympicsRpcAttribute)))
                    continue;
                var rpcMethod = new RpcMethod(method, observable);
                _rpcMethods.Add(_methodIdCounter, rpcMethod);
                _rpcMethodIds.Add(rpcMethod, _methodIdCounter);
                _methodIdCounter++;
            }
        }

        public ushort GetIdOf(RpcMethod rpcMethod) => _rpcMethodIds[rpcMethod];

        internal void Clear()
        {
            _rpcMethods.Clear();
            _rpcMethodIds.Clear();
        }
    }
}
