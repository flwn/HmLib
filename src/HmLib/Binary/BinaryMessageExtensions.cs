using System;
using System.Threading;
using System.Threading.Tasks;

namespace HmLib.Binary
{
    using Abstractions;
    using Serialization;

    public static class BinaryMessageExtensions
    {

        public static async Task<BinaryResponse> ReadAsBinary(this IResponseMessage responseMessage, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new BinaryResponse();

            await ReadAsBinary(() => responseMessage.GetMessageReader(), responseMessage, result, cancellationToken);

            return result;
        }

        public static async Task<BinaryRequest> ReadAsBinary(this IRequestMessage requestMessage, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new BinaryRequest();

            await ReadAsBinary(() => requestMessage.GetMessageReader(), requestMessage, result, cancellationToken);

            return result;
        }

        private static async Task ReadAsBinary(Func<IMessageReader> readerFunc, object message, BinaryMessage result, CancellationToken cancellationToken)
        {
            var fastCopy = message as IFastCopyTo<BinaryMessage>;
            if (fastCopy != null)
            {
                await fastCopy.CopyTo(result, cancellationToken);
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
