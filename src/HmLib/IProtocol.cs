using System.IO;

namespace HmLib
{
    public interface IProtocol
    {
        Response ReadResponse(Stream inputStream);
        void WriteRequest(Stream outputStream, Request request);
    }
}