using System;

namespace WebRtcWrapper
{
    public class WebRtcException : Exception
    {
        public WebRtcException(string message) : base(message)
        { }
    }
}
