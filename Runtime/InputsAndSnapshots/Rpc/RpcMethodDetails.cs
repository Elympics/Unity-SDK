namespace Elympics
{
    internal readonly struct RpcMethodDetails
    {
        public uint? MetadataParameterIndex { get; init; }

        public RpcMethodDetails(RpcMethod method)
        {
            MetadataParameterIndex = null;
            var parameters = method.MethodInfo.GetParameters();
            for (var i = 0u; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.ParameterType == typeof(RpcMetadata)
                    && parameter.IsOptional
                    && parameter.HasDefaultValue
                    && (parameter.DefaultValue == null || (RpcMetadata)parameter.DefaultValue == default))
                {
                    MetadataParameterIndex = i;
                    break;
                }
            }
        }
    }
}
