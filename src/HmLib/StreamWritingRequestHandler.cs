using System;
using System.IO;
using System.Threading.Tasks;

namespace HmLib
{
    using Abstractions;
    using Binary;

    internal class StreamWritingRequestHandler : RequestHandler
    {
        private readonly Stream _innerStream;

        public StreamWritingRequestHandler(Stream innerStream)
        {
            if (innerStream == null) throw new ArgumentNullException(nameof(innerStream));

            _innerStream = innerStream;
        }

        internal protected override async Task<IResponseMessage> HandleRequest(IRequestMessage requestMessage)
        {
            if (requestMessage == null) throw new ArgumentNullException(nameof(requestMessage));

            var binaryRequest = await requestMessage.ReadAsBinary();

            await binaryRequest.MessageStream.CopyToAsync(_innerStream);

            await Task.Delay(100);

            return new BinaryResponse(_innerStream);
        }

    }
}
