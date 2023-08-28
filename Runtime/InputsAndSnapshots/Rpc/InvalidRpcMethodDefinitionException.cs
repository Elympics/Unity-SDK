namespace Elympics
{
    public class InvalidRpcMethodDefinitionException : ElympicsException
    {
        public InvalidRpcMethodDefinitionException(string message)
            : base(message)
        { }
    }
}
