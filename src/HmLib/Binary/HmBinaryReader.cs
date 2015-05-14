using System;
using System.IO;
using System.Net;
using System.Text;

namespace HmLib.Binary
{
    public class HmBinaryReader : BinaryReader
    {
        private class ReadCounterStream : Stream
        {
            private readonly Stream _wrappingStream;

            public long BytesRead { get; private set; }

            public ReadCounterStream(Stream wrappingStream)
            {
                _wrappingStream = wrappingStream;
            }

            public override void Flush()
            {
                _wrappingStream.Flush();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _wrappingStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _wrappingStream.SetLength(value);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                BytesRead += count;
                return _wrappingStream.Read(buffer, offset, count);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _wrappingStream.Write(buffer, offset, count);
            }

            public override bool CanRead
            {
                get { return _wrappingStream.CanRead; }
            }

            public override bool CanSeek
            {
                get { return _wrappingStream.CanSeek; }
            }

            public override bool CanWrite
            {
                get { return _wrappingStream.CanWrite; }
            }

            public override long Length
            {
                get { return _wrappingStream.Length; }
            }

            public override long Position
            {
                get { return _wrappingStream.Position; }
                set { _wrappingStream.Position = value; }
            }
        }

        private const byte TrueByte = 0x01;
        private const byte FalseByte = 0x00;


        private static readonly Encoding Engcoding = Encoding.ASCII;

        public HmBinaryReader(Stream input, bool leaveOpen = true)
            : base(new ReadCounterStream(input), Encoding.ASCII, leaveOpen)
        {
        }


        public override int ReadInt32()
        {
            var result = base.ReadInt32();

            return IPAddress.NetworkToHostOrder(result);
        }

        public override string ReadString()
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


        public override double ReadDouble()
        {
            var mantissa = (double)ReadInt32();
            var exponent = (double)ReadInt32();

            var floatValue = mantissa / (double)0x40000000;
            floatValue *= Math.Pow(2, exponent);
            return floatValue;
        }

        public override bool ReadBoolean()
        {
            var booleanByte = ReadByte();

            if (booleanByte == TrueByte)
            {
                return true;
            }
            if (booleanByte == FalseByte)
            {
                return false;
            }
            throw new InvalidOperationException();
        }

        public override float ReadSingle()
        {
            throw new InvalidOperationException("Use ReadDouble.");
        }

        public long BytesRead { get { return ((ReadCounterStream)BaseStream).BytesRead; } }
    }
}