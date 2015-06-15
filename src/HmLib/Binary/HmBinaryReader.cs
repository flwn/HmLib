using System;
using System.IO;
using System.Text;

namespace HmLib.Binary
{
    using Serialization;

    public class HmBinaryReader : IHmStreamReader
    {
        private static readonly Encoding Encoding = Encoding.ASCII;

        private long _bytesReadTotal = 0L;
        private Stream _input;

        public HmBinaryReader(Stream input, bool leaveOpen = true)
        {
            _input = input;
        }

        public ContentType ReadContentType()
        {
            var contentType = (ContentType)ReadInt32();

            return contentType;
        }

        public int ReadInt32()
        {
            var intBuffer = ReadBytes(4);
            return HmBitConverter.ToInt32(intBuffer);
        }

        public string ReadString()
        {
            var stringLength = ReadInt32();

            if (stringLength == 0)
            {
                return string.Empty;
            }

            var stringBytes = ReadBytes(stringLength);

            var stringValue = Encoding.GetString(stringBytes);

            return stringValue;
        }


        public double ReadDouble()
        {
            var doubleBytes = ReadBytes(8);
            return HmBitConverter.ToDouble(doubleBytes);
        }

        public bool ReadBoolean()
        {
            var booleanByte = ReadBytes(1);

            return HmBitConverter.ToBoolean(booleanByte, 0);
        }

        public byte ReadByte()
        {
            _bytesReadTotal++;
            var @byte = _input.ReadByte();
            if (@byte == -1)
            {
                throw new EndOfStreamException();
            }
            return (byte)@byte;
        }

        public byte[] ReadBytes(int count)
        {

            var buffer = new byte[count];
            var bytesRead = 0;
            do
            {
                var read = _input.Read(buffer, bytesRead, count - bytesRead);

                _bytesReadTotal += read;

                if (read == 0)
                {
                    throw new EndOfStreamException(string.Format("Read {0} bytes, expected {0} bytes.", bytesRead, count));
                }

                bytesRead += read;

                //loop until buffer is filled in case the stream has not catched up yet.
            } while (bytesRead < count);


            return buffer;
        }

        public long BytesRead { get { return _bytesReadTotal; } }

    }
}