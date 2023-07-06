using System;

namespace Elympics
{
    public class ReadNotEnoughException : Exception
    {
        public ReadNotEnoughException(ElympicsBehaviour elympicsBehaviour)
            : base($"Didn't read all bytes associated with input for network id {elympicsBehaviour.networkId}. Make sure you read all the values you wrote when reading.")
        {
        }
    }
}
