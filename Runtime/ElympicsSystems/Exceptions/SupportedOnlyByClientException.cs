namespace Elympics
{
    public class SupportedOnlyByClientException : ElympicsException
    {
        private const string DefaultMessage = "This method is supported only for client";

        public SupportedOnlyByClientException() : base(DefaultMessage)
        { }

        public SupportedOnlyByClientException(string message) : base(message)
        { }
    }
}
