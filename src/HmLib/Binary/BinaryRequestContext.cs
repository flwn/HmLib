using System.IO;

namespace HmLib.Binary
{
    using Abstractions;

    public class BinaryRequestContext : IRequestContext
    {
        private Stream _requestStream;
        private Stream _responseStream;

        public BinaryRequestContext(Stream requestResponseStream)
            : this(requestResponseStream, requestResponseStream)
        {
        }

        public BinaryRequestContext(Stream requestStream, Stream responseStream)
        {
            _requestStream = requestStream;
            _responseStream = responseStream;
            Request = new Binary.HmBinaryMessageReader(requestStream);
            Response = new Binary.HmBinaryMessageWriter(responseStream);
        }



        public IMessageReader Request
        {
            get; private set;
        }

        public IMessageBuilder Response
        {
            get; private set;
        }

        public string Protocol { get { return "binary"; } }

        public Stream GetRequestStream()
        {
            return _requestStream;
        }
        public Stream GetResponseStream()
        {
            return _responseStream;
        }
    }
}
