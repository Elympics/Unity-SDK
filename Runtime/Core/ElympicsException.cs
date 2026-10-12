using System;

namespace Elympics
{
    public class ElympicsException : Exception
    {
        public ElympicsException(string message)
            : base(message)
        { }

        public ElympicsException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
