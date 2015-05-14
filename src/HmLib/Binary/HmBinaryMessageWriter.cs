using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HmLib.Binary
{
    public class HmBinaryMessageWriter
    {
        private static readonly byte[] PacketHeader = Encoding.ASCII.GetBytes("Bin");

        private enum PacketType : byte
        {
            BinaryRequest = 0x00,
            BinaryResponse = 0x01,
            BinaryRequestHeader = 0x40,
            BinaryResponseHeader = 0x41,
            ErrorResponse = 0xff,
        }


        private readonly HmSerializer _headerSerializer = new HmSerializer { WriteTypeInfoLevel = HmSerializer.WriteTypesFor.Nothing };
        private readonly HmSerializer _bodySerializer = new HmSerializer { WriteTypeInfoLevel = HmSerializer.WriteTypesFor.ChildElements };

        public HmBinaryMessageWriter()
        {
        }


        public void WriteRequest(Stream outputStream, HmRpcClient.Request request)
        {
            var writer = new HmBinaryWriter(outputStream);

            writer.Write(PacketHeader);


            if (request.Headers.Count > 0)
            {
                writer.Write((byte)PacketType.BinaryRequestHeader);

                SerializeHeaders(writer, request.Headers);
            }
            else
            {
                writer.Write((byte)PacketType.BinaryRequest);
            }

            SerializeContent(writer, request.Method, request.Parameters);
        }



        private void SerializeHeaders(HmBinaryWriter writer, IDictionary<string, string> headerDictionary)
        {
            using (var bufferedWriter = HmBinaryWriter.Buffered(writer))
            {
                _headerSerializer.Serialize(bufferedWriter, headerDictionary);

                bufferedWriter.Flush();
            }
        }

        private void SerializeContent(HmBinaryWriter writer, string methodName, ICollection<object> parameters)
        {
            using (var bufferedWriter = HmBinaryWriter.Buffered(writer))
            {
                _headerSerializer.Serialize(bufferedWriter, methodName);

                _bodySerializer.Serialize(bufferedWriter, parameters);

                bufferedWriter.Flush();
            }
        }
    }
}