using System;
using System.Threading.Tasks;

namespace HmLib.Binary
{
    using Abstractions;
    using Serialization;

    public static class BinaryMessageExtensions
    {

        public static Task<BinaryResponse> ReadAsBinary(this IResponseMessage responseMessage)
        {
            var result = new BinaryResponse();

            ReadAsBinary(() => responseMessage.GetMessageReader(), responseMessage, result);

            return Task.FromResult(result);
        }

        public static Task<BinaryRequest> ReadAsBinary(this IRequestMessage requestMessage)
        {
            var result = new BinaryRequest();

            ReadAsBinary(() => requestMessage.GetMessageReader(), requestMessage, result);

            return Task.FromResult(result);
        }

        private static void ReadAsBinary(Func<IMessageReader> readerFunc, object message, BinaryMessage result)
        {
            var fastCopy = message as IFastCopyTo<BinaryMessage>;
            if (fastCopy != null)
            {
                fastCopy.CopyTo(result);
            }
            else
            {
                var reader = readerFunc();
                var writer = new HmBinaryMessageWriter(result);
                Transformer.Transform(reader, writer);
            }

            //rewind memory stream for reading
            result.MessageStream.Position = 0L;
        }

    }
}
