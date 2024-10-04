using System;

namespace SCS
{
    public class InvalidSmartContractTypeException : Exception
    {
        public InvalidSmartContractTypeException(string message) : base(message)
        { }
    }

}
