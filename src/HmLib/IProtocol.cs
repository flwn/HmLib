using System.IO;

namespace HmLib
{
    using Serialization;

    public interface IProtocol
    {
        void ReadResponse(Stream inputStream, IMessageBuilder output);

        void WriteRequest(Stream outputStream, Request request);
    }
}