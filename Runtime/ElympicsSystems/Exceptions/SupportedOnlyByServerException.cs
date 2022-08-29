namespace Elympics
{
	public class SupportedOnlyByServerException : ElympicsException
	{
		private const string DefaultMessage = "This method is supported only for server";

		public SupportedOnlyByServerException() : base(DefaultMessage)
		{ }

		public SupportedOnlyByServerException(string message) : base(message)
		{ }
	}
}
