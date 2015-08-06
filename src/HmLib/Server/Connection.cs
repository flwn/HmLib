using System;
using System.IO;
using System.Threading.Tasks;

namespace HmLib.Server
{
    using Binary;

    public class Connection : IDisposable
    {
        private RequestDispatcher _requestDispatcher;

        public Connection(ClientConnectionInfo info, Stream stream, RequestDispatcher requestDispatcher)
        {
            Info = info;
            Stream = stream;
            _requestDispatcher = requestDispatcher;
        }

        public ClientConnectionInfo Info { get; }


        public Stream Stream { get; }

        public void Dispose()
        {
            Stream.Dispose();
        }

        public async Task Handle()
        {

            var message = (BinaryRequest)await BinaryMessage.ReadFromStream(Stream);

            var response = await _requestDispatcher.Dispatch(message);

            var binaryResponse = await response.ReadAsBinary();

            await binaryResponse.MessageStream.CopyToAsync(Stream);

        }
    }
}