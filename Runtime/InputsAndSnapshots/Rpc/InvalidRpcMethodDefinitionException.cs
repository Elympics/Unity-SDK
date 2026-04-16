namespace Elympics
{
    public class InvalidRpcMethodDefinitionException : ElympicsException
    {
        public InvalidRpcMethodDefinitionException(string methodName, string message)
            : base($"RPC method {methodName} has invalid definition! {message}")
        { }

        public static InvalidRpcMethodDefinitionException NotElympicsSubclass(string methodName) =>
            new(methodName, $"RPC method declaring type has to be a subclass of {nameof(ElympicsMonoBehaviour)}");

        public static InvalidRpcMethodDefinitionException Static(string methodName) =>
            new(methodName, "RPC method cannot be static");

        public static InvalidRpcMethodDefinitionException Virtual(string methodName) =>
            new(methodName, "RPC method cannot be virtual or abstract");

        public static InvalidRpcMethodDefinitionException NonVoidReturn(string methodName) =>
            new(methodName, "RPC method must return void");

        public static InvalidRpcMethodDefinitionException Generic(string methodName) =>
            new(methodName, "RPC method cannot be generic");

        public static InvalidRpcMethodDefinitionException Overloaded(string methodName) =>
            new(methodName, "RPC method cannot have an overload");
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
