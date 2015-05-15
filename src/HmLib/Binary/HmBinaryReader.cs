using System;
using System.IO;
using System.Net;
using System.Text;

namespace HmLib.Binary
{
    public class HmBinaryReader
    {
        private long _bytesRead = 0L;

        private static readonly Encoding Engcoding = Encoding.ASCII;
        private Stream _input;

        public HmBinaryReader(Stream input, bool leaveOpen = true)
        {
            _input = input;
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
          
            var stringValue = Engcoding.GetString(stringBytes);

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
            _bytesRead++;
            return (byte)_input.ReadByte();
        }

        public byte[] ReadBytes(int count)
        {

            var buffer = new byte[count];
            var read = _input.Read(buffer, 0, count);
            _bytesRead += read;
            if (read != count)
            {
                throw new InvalidOperationException("");
            }
            return buffer;
        }

        public long BytesRead { get { return _bytesRead; } }
    }
}