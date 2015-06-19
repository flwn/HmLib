using HmLib.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HmLib.Binary
{
    internal class HmBinaryStreamReader : IDisposable
    {

        private readonly Stream _inStream;
        private readonly bool _closeOnDispose;
        private bool _isDisposed;
        private long _bytesReadTotal = 0L;

        private static readonly Encoding Encoding = Encoding.ASCII;

        public HmBinaryStreamReader(Stream input, bool closeOnDispose = false)
        {
            _inStream = input;
            _closeOnDispose = closeOnDispose;
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
            var @byte = _inStream.ReadByte();
            if (@byte == -1)
            {
                throw new EndOfStreamException();
            }
            return (byte)@byte;
        }

        public byte[] ReadBytes(int count)
        {
            var buffer = new byte[count];

            if (count == 0)
            {
                return buffer;
            }

            var bytesRead = 0;
            do
            {
                var read = _inStream.Read(buffer, bytesRead, count - bytesRead);

                _bytesReadTotal += read;

                if (read == 0)
                {
                    throw new EndOfStreamException(string.Format("Read {0} bytes, expected {1} bytes.", bytesRead, count));
                }

                bytesRead += read;

                //loop until buffer is filled in case the stream has not catched up yet.
            } while (bytesRead < count);


            return buffer;
        }

        public long BytesRead { get { return _bytesReadTotal; } }



        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (!disposing)
            {
                return;
            }

            if (_closeOnDispose)
            {
                using (_inStream) { }
            }

            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
