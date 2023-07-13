using System;

namespace Elympics
{
    public class ReadTooMuchException : Exception
    {
        public ReadTooMuchException(ElympicsBehaviour elympicsBehaviour)
            : base($"Read too many bytes when reading input for network id {elympicsBehaviour.networkId}. Make sure you write all the values you want to read.")
        {
        }
    }
}
