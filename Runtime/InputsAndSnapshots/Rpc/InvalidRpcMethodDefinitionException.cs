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
        public UnsupportedParameterTypeException(string methodName, int parameterIndex, string parameterName, string parameterTypeName)
            : base(methodName, $"Parameter {parameterIndex + 1}. {parameterName} is of unsupported type: {parameterTypeName}")
        { }
    }

    public class InvalidRpcMetadataParameterDefinitionException : InvalidRpcMethodDefinitionException
    {
        private InvalidRpcMetadataParameterDefinitionException(string methodName, string reason)
            : base(methodName, reason)
        { }

        public static InvalidRpcMetadataParameterDefinitionException FromNonOptional(string methodName, int parameterIndex, string parameterName) =>
            new(methodName, $"Parameter {parameterIndex + 1}. {parameterName} of type {nameof(RpcMetadata)} must be optional");

        public static InvalidRpcMetadataParameterDefinitionException FromDuplicated(string methodName, int parameterIndex, string parameterName, int previousParameterIndex, string previousParameterName) =>
            new(methodName, $"Parameter of type {nameof(RpcMetadata)} is defined too many times: "
                + $"{parameterIndex + 1}. {parameterName} after {previousParameterIndex + 1}. {previousParameterName}");
    }
}
