using System;
using System.IO;
using System.Text;
using System.Threading;

namespace HmLib.Binary
{
    public class HmBinaryWriter : IDisposable, IHmStreamWriter
    {
        private readonly Stream _outStream;
        private readonly bool _closeOnDispose;
        private bool _isDisposed;
        private static readonly Encoding Encoding = Encoding.ASCII;

        internal long Length { get { return _outStream.Length; } }

        public HmBinaryWriter(Stream output, bool closeOnDispose = false)
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

        public void Write(ContentType contentType)
        {
            Write((int)contentType);
        }

        public void Write(float value)
        {
            Write((double)value);
        }

        public void Write(double value)
        {
            var endianCorrectValue = HmBitConverter.GetBytes(value);
            Write(endianCorrectValue);
        }

        public void Write(int value)
        {
            var endianCorrectValue = HmBitConverter.GetBytes(value);
            Write(endianCorrectValue);
        }

        public void Write(bool value)
        {
            var endianCorrectValue = HmBitConverter.GetBytes(value);
            Write(endianCorrectValue);
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
                Write(bytesValue);
            }
        }

        public void Write(byte value)
        {
            OutStream.WriteByte(value);
        }

        public void Write(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            
            OutStream.Write(value, 0, value.Length);
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

        public virtual void Flush()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("HmBinaryWriter");
            }
        }


        internal sealed class BufferedWriter : HmBinaryWriter
        {
            private readonly HmBinaryWriter _wrappedWriter;
            private readonly bool _writeLengthUpfront;


            public BufferedWriter(HmBinaryWriter wrappedWriter, bool writeLengthUpfront)
                : base(new MemoryStream(), true)
            {
                _wrappedWriter = wrappedWriter;
                _writeLengthUpfront = writeLengthUpfront;
            }

            public override void Flush()
            {
                if (_writeLengthUpfront)
                {
                    _wrappedWriter.Write((int)_outStream.Length);
                }

                ((MemoryStream)OutStream).WriteTo(_wrappedWriter._outStream);
            }

        }

        public static HmBinaryWriter Buffered(HmBinaryWriter wrappedWriter, bool writeLengthUpfront = true)
        {
            var bufferedWriter = new BufferedWriter(wrappedWriter, writeLengthUpfront);
            
            return bufferedWriter;
        }
    }

}