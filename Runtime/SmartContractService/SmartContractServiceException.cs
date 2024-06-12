using System;

namespace SCS
{
    public class SmartContractServiceException : Exception
    {
        public SmartContractServiceException(string message) : base(message)
        { }
    }

}
