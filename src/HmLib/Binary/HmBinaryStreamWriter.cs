using System;
using System.IO;
using System.Text;

namespace HmLib.Binary
{
    internal class HmBinaryStreamWriter : IDisposable
    {
        private readonly Stream _outStream;
        private readonly bool _closeOnDispose;
        private bool _isDisposed;

        private static readonly Encoding Encoding = Encoding.ASCII;

        internal long Length { get { return _outStream.Length; } }


        public HmBinaryStreamWriter(Stream output, bool closeOnDispose = false)
        {
            _outStream = output;
            _closeOnDispose = closeOnDispose;
        }

        protected Stream OutStream
        {
            get
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException("HmBinaryWriter");
                }
                return _outStream;
            }
        }


        public void Write(bool value)
        {
            var endianCorrectValue = HmBitConverter.GetBytes(value);
            WriteRaw(endianCorrectValue);
        }

        public void Write(ContentType contentType)
        {
            Write((int)contentType);
        }


        public void Write(double value)
        {
            var endianCorrectValue = HmBitConverter.GetBytes(value);
            WriteRaw(endianCorrectValue);
        }

        public void Write(int value)
        {
            var endianCorrectValue = HmBitConverter.GetBytes(value);
            WriteRaw(endianCorrectValue);
        }

        public void Write(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var bytesValue = Encoding.GetBytes(value);

            Write(bytesValue.Length);

            if (bytesValue.Length > 0)
            {
                WriteRaw(bytesValue);
            }
        }

        public void WriteRaw(byte value)
        {
            OutStream.WriteByte(value);
        }

        public void WriteRaw(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            OutStream.Write(value, 0, value.Length);
        }


        public void WriteTo(HmBinaryStreamWriter other)
        {
            ((MemoryStream)_outStream).WriteTo(other._outStream);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (!disposing)
            {
                return;
            }

            if (_closeOnDispose)
            {
                using (_outStream) { }
            }

            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public override string ToString()
        {
            var buffer = ((MemoryStream)OutStream).ToArray();
            var output = new StringBuilder(buffer.Length * 2);
            foreach (var @byte in buffer)
            {
                output.AppendFormat("{0:X2}", @byte);
            }

            return output.ToString();
        }
    }

}