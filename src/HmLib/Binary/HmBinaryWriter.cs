using System;
using System.IO;
using System.Text;
using System.Threading;

namespace HmLib.Binary
{
    using Serialization;

    public class HmBinaryWriter : IDisposable, IHmStreamWriter, IObjectBuilder
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

        public void Write(bool value)
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

        public void BeginArray(int? count = default(int?))
        {
            Write(ContentType.Array);

            if (count.HasValue)
            {
                Write(count.Value);
            }
        }

        public void BeginItem()
        {
        }

        public void BeginStruct(int? count = default(int?))
        {
            Write(ContentType.Struct);

            if (count.HasValue)
            {
                WriteInt32Value(count.Value);
            }
        }

        public void EndArray()
        {
        }

        public void EndItem()
        {
        }

        public void EndStruct()
        {
        }

        public void WriteBase64String(string base64String)
        {
            Write(ContentType.Base64);
            Write(base64String);
        }

        public void WriteBooleanValue(bool value)
        {
            Write(ContentType.Boolean);
            Write(value);
        }

        public void WriteDoubleValue(double value)
        {
            Write(ContentType.Float);
            Write(value);
        }

        public void WriteInt32Value(int value)
        {
            Write(ContentType.Int);
            Write(value);
        }

        public void WritePropertyName(string name)
        {
            Write(name);
        }

        public void WriteStringValue(string value)
        {
            Write(ContentType.String);
            Write(value);
        }

        public override string ToString()
        {
            var buffer = ((MemoryStream)OutStream).ToArray();
            var output = new StringBuilder(buffer.Length * 2);
            foreach (var byte1 in buffer)
            {
                output.AppendFormat("{0:X2}", byte1);
            }

            return output.ToString();
        }
    }

}