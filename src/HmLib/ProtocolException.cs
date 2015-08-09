using System;

namespace HmLib
{
#if DNX451
    [Serializable]
#endif
    public class ProtocolException : Exception
    {
        public ProtocolException() { }
        public ProtocolException(string message) : base(message) { }
        public ProtocolException(string message, Exception inner) : base(message, inner) { }

        public int Code { get; set; } = int.MinValue;

#if DNX451
        protected ProtocolException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        { }
#endif
    }
}
