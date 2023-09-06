using System;

namespace Elympics
{
    public class ElympicsException : Exception
    {
        public ElympicsException(string message)
            : base(ElympicsLogger.PrependWithDetails(message))
        { }

        public ElympicsException(string message, Exception innerException)
            : base(ElympicsLogger.PrependWithDetails(message), innerException)
        { }
    }
}
