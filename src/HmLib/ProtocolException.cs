﻿using System;

namespace HmLib
{
    [Serializable]
    public class ProtocolException : Exception
    {
        public ProtocolException() { }
        public ProtocolException(string message) : base(message) { }
        public ProtocolException(string message, Exception inner) : base(message, inner) { }
        protected ProtocolException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        { }
    }
}
