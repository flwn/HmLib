using System.Linq;
using System.IO;
using System.Text;
using System;
using System.Threading.Tasks;

namespace HmLib.Binary
{
    internal class Utils
    {

        public async static Task<byte[]> ReadMessageStreamWithLengthVerification(Stream stream)
        {
            var buffer = new byte[1024];

            var position = await stream.ReadAsync(buffer, 0, 4);

            if (position != 4)
            {
                throw new ProtocolException("Expected 4 bytes");
            }

            if (!buffer.Take(3).SequenceEqual(Packet.PacketHeader))
            {
                throw new ProtocolException("Invalid PacketHeader");
            }

            var containsHeaders = (buffer[3] != Packet.ErrorMessage) &&
                (buffer[3] & Packet.MessageContainsHeaders) == Packet.MessageContainsHeaders;

            var result = Tuple.Create(buffer, position);

            if (containsHeaders)
            {
                result = await ReadContent(stream, result.Item1, result.Item2);
                buffer = result.Item1;
                position = result.Item2;

                if (position + 4 > buffer.Length)
                {
                    //make the array large enough so at least the length of the content fit into the buffer
                    Array.Resize(ref buffer, position + 4);
                }
            }

            result = await ReadContent(stream, result.Item1, result.Item2);
            buffer = result.Item1;
            position = result.Item2;

            if (position != buffer.Length)
            {
                Array.Resize(ref buffer, position);
            }

            return buffer;
        }

        /// <summary>
        /// Using tuple because ref-params are not supported async.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static async Task<Tuple<byte[], int>> ReadContent(Stream stream, byte[] buffer, int offset)
        {
            var read1 = await stream.ReadAsync(buffer, offset, 4);
            if (read1 != 4)
            {
                throw new ProtocolException($"Expected 4 bytes, got {read1} bytes.");
            }

            var contentLength = HmBitConverter.ToInt32(buffer, offset);

            offset += read1;

            if (contentLength + offset > buffer.Length)
            {
                Array.Resize(ref buffer, contentLength + offset);
            }

            var totalRead = 0;
            do
            {
                var read2 = await stream.ReadAsync(buffer, offset, contentLength - totalRead);
                offset += read2;
                totalRead += read2;
                if (read2 == 0)
                {
                    break;
                }
            }
            while (totalRead < contentLength);


            if (totalRead != contentLength)
            {
                throw new ProtocolException($"Expected content length of {contentLength} bytes, got {totalRead} bytes.");
            }

            return Tuple.Create(buffer, offset);
        }


        /// <summary>
        /// Tokenize a bin rpc stream for debugging purposes.
        /// </summary>
        /// <param name="buffer">The raw buffer with bin rpc data.</param>
        /// <returns>The stream data in a more human-readable format.</returns>
        public static string Tokenize(byte[] buffer)
        {
            using (var memstream = new MemoryStream(buffer))
            {
                var reader = new HmBinaryStreamReader(memstream);
                return Tokenize(reader);
            }
        }

        /// <summary>
        /// Tokenize a bin rpc stream for debugging purposes.
        /// </summary>
        /// <param name="streamReader">The reader which will read the raw stream.</param>
        /// <returns>The stream data in a more human-readable format.</returns>
        public static string Tokenize(HmBinaryStreamReader streamReader)
        {
            int state = 0;

            var outB = new StringBuilder();
            streamReader.ReadBytes(3);
            var packetType = streamReader.ReadByte();
            outB.AppendFormat("Bin 0x{0:X2} ", packetType);
            var contentLength = streamReader.ReadInt32();
            outB.AppendFormat("{0} ", contentLength);

            if (packetType == 0x00 || packetType == 0x40)
            {
                //request
                state = 2;
            }

            if (packetType != 0xff && (packetType | 0x40) == 0x40)
            {
                streamReader.ReadBytes(contentLength);
                contentLength = streamReader.ReadInt32();
                outB.AppendFormat("HEADERS_SKIPPED {0} ", contentLength);
            }

            var expected = (int)streamReader.BytesRead + contentLength;
            if (contentLength == 0)
            {
                return outB.ToString();
            }
            return tokenize(streamReader, outB, state, expected, false);
        }
        private static string tokenize(HmBinaryStreamReader r, StringBuilder outB, int state, int expected, bool returnAfterLoop)
        {
            do
            {
                var ct = state == 0 ? r.ReadContentType()
                    : state-- == 2 ? ContentType.String : ContentType.Array;

                switch (ct)
                {
                    case ContentType.Int:
                        outB.AppendFormat("i'{0}' ", r.ReadInt32());
                        break;
                    case ContentType.Boolean:
                        outB.AppendFormat("b'{0}' ", r.ReadBoolean());
                        break;
                    case ContentType.String:
                        outB.AppendFormat("s'{0}' ", r.ReadString());
                        break;
                    case ContentType.Float:
                        outB.AppendFormat("f'{0}' ", r.ReadDouble());
                        break;
                    case ContentType.Base64:
                        outB.AppendFormat("b64'{0}' ", r.ReadString());
                        break;
                    case ContentType.Array:
                        var arrItems = r.ReadInt32();
                        outB.AppendFormat("array({0})[ ", arrItems);
                        while (arrItems-- > 0)
                        {
                            tokenize(r, outB, state, expected, true);
                        }
                        outB.Append("] ");
                        break;
                    case ContentType.Struct:
                        var itemCount = r.ReadInt32();
                        outB.AppendFormat("struct({0})[ ", itemCount);
                        while (itemCount-- > 0)
                        {
                            outB.AppendFormat("{0}=", r.ReadString());
                            tokenize(r, outB, state, expected, true);
                        }
                        outB.Append("] ");
                        break;
                    default:
                        outB.Append("?? ");
                        break;
                }

                if (returnAfterLoop) break;

            } while (r.BytesRead < expected && state >= 0);

            return outB.ToString();
        }
    }
}
