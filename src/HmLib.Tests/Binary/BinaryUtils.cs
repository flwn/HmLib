using HmLib.Binary;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace HmLib.Tests.Binary
{
    internal class BinaryUtils
    {

        public static MemoryStream CreateByteStream(string messageByteStream)
        {
            var messageBytes = Enumerable.Range(0, messageByteStream.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(messageByteStream.Substring(x, 2), 16))
                     .ToArray();

            var messageStream = new MemoryStream(messageBytes);

            return messageStream;
        }

        public static string FormatByteArray(byte[] byteArray)
        {
            var outputFormatted = string.Join("", byteArray.Select(x => x.ToString("X2")));

            return outputFormatted;
        }
        public static string FormatMemoryStream(MemoryStream output)
        {
            var outputBuffer = output.ToArray();

            return FormatByteArray(outputBuffer);

        }

        public static string Tokenize(HmBinaryStreamReader r)
        {
            int state = 0;

            var outB = new StringBuilder();
            r.ReadBytes(3);
            var packetType = r.ReadByte();
            outB.AppendFormat("Bin 0x{0:X2} ", packetType);
            var contentLength = r.ReadInt32();

            var expected = (int)r.BytesRead + contentLength;
            outB.AppendFormat("{0} ", contentLength);

            if (packetType == 0x00)
            {
                //request
                state = 2;
            }
            return tokenize(r, outB, state, expected, false);
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
