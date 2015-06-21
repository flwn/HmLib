using System.IO;
using System.Text;

namespace HmLib.Binary
{
    internal class Utils
    {

        /// <summary>
        /// Tokenize a bin rpc stream for debugging purposes.
        /// </summary>
        /// <param name="buffer">The raw buffer with bin rpc data.</param>
        /// <returns>The stream data in a more human-readable format.</returns>
        public static string Tokenize(byte[] buffer)
        {
            using(var memstream = new MemoryStream(buffer))
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
