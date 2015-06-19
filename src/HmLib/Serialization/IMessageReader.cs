using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HmLib.Serialization
{
    public enum HmMessagePart
    {
        None,
        Message,
        Headers,
        Body,
        EndOfFile,
    }
    public interface IMessageReader
    {

        bool Read();

        HmMessagePart MessagePart { get; }
        MessageType MessageType { get; }


        int HeaderCount { get; }
        string Key { get; }
        string Value { get; }

        ContentType? ValueType { get; }

    }
}
