using System;
using System.IO;
using System.Linq;

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
        public static string Tokenize(Stream input)
        {
            var reader = new HmLib.Binary.HmBinaryStreamReader(input);

            return HmLib.Binary.Utils.Tokenize(reader);
        }
    }
}
