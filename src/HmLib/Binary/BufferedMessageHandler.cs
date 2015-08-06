using System;
using System.IO;
using System.Threading.Tasks;

namespace HmLib.Binary
{
    using Abstractions;

    public class BufferedMessageHandler : DelegatingRequestHandler
    {
        public BufferedMessageHandler(RequestHandler next) : base(next)
        {
        }

        internal protected override async Task<IResponseMessage> HandleRequest(IRequestMessage requestMessage)
        {
            try
            {
                var bufferedRequest = await requestMessage.ReadAsBinary();

                var response = await base.HandleRequest(bufferedRequest);

                var binaryResponse = response as BinaryResponse;

                if (binaryResponse == null)
                {
                    binaryResponse = await response.ReadAsBinary();
                }
                else
                {
                    var buffer = await Utils.ReadMessageIntoBuffer(binaryResponse.MessageStream);
                    var stream = new MemoryStream(buffer.Item1);
                    var bufferedResponse = new BinaryResponse(stream);
                    return bufferedResponse;
                }

                return binaryResponse;
            }
            catch (ProtocolException protocolException)
            {
                var errorBuffer = new MemoryStream();
                var response = new BinaryResponse(errorBuffer);
                var writer = new HmBinaryMessageWriter(response);
                writer.BeginMessage(MessageType.Error);
                writer.BeginContent();
                writer.BeginStruct(2);
                writer.BeginItem();
                writer.WritePropertyName("faultCode");
                writer.WriteInt32Value(int.MinValue);
                writer.EndItem();
                writer.BeginItem();
                writer.WritePropertyName("faultMessage");
                writer.WriteStringValue(protocolException.Message);
                writer.EndItem();
                writer.EndStruct();
                writer.EndContent();
                writer.EndMessage();

#if DEBUG
                errorBuffer.Seek(0, SeekOrigin.Begin);
                var intBuf = new byte[4];
                errorBuffer.Read(intBuf, 4, 4);
                var length = HmBitConverter.ToInt32(intBuf);
                if (errorBuffer.Length != length + 4)
                {
                    throw new ProtocolException("Error message not correct.");
                }
#endif

                errorBuffer.Position = 0L;
                return response;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                throw;

            }
        }

    }
}
