using Mono.Cecil;

namespace Elympics
{
    public class InvalidRpcMethodDefinitionException : ElympicsException
    {
        public InvalidRpcMethodDefinitionException(string methodName, string message)
            : base($"RPC method {methodName} has invalid definition! {message}")
        { }
    }

    public class UnsupportedParameterTypeException : InvalidRpcMethodDefinitionException
    {
        public UnsupportedParameterTypeException(string methodName, ParameterDefinition parameter)
            : base(methodName, $"Parameter {parameter.Index + 1}. {parameter.Name} is of unsupported type: {parameter.ParameterType.FullName}")
        { }
    }

    public class InvalidRpcMetadataParameterDefinitionException : InvalidRpcMethodDefinitionException
    {
        private InvalidRpcMetadataParameterDefinitionException(string methodName, string reason)
            : base(methodName, reason)
        { }

        public static InvalidRpcMetadataParameterDefinitionException FromNonOptional(string methodName, ParameterDefinition parameter) =>
            new(methodName, $"Parameter {parameter.Index + 1}. {parameter.Name} of type {nameof(RpcMetadata)} must be optional");

        public static InvalidRpcMetadataParameterDefinitionException FromDuplicated(string methodName, ParameterDefinition previousParameter, ParameterDefinition currentParameter) =>
            new(methodName, $"Parameter of type {nameof(RpcMetadata)} is defined too many times: "
                + $"{currentParameter.Index + 1}. {currentParameter.Name} after {previousParameter.Index + 1}. {previousParameter.Name}");
    }
}
