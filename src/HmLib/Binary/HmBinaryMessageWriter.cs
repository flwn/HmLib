using System;
using System.IO;
using System.Text;

namespace HmLib.Binary
{
    public class HmBinaryMessageWriter : IDisposable, Serialization.IMessageBuilder
    {
        private static readonly byte[] PacketHeader = Encoding.ASCII.GetBytes("Bin");

        private const byte ErrorMessage = 0xff;
        private const byte RequestMessage = 0x00;
        private const byte ResponseMessage = 0x01;
        private const byte MessageContainsHeaders = 0x40;


        private readonly HmBinaryStreamWriter _outputWriter;
        private HmBinaryStreamWriter _writeBuffer;

        private MessageType _messageType;
        private byte _packetType;
        private int _paramDepth;

        private int _headerCount;
        private int _headersWritten;


        public HmBinaryMessageWriter(Stream output, bool closeOnDispose = false)
        {
            _writeBuffer = _outputWriter = new HmBinaryStreamWriter(output, closeOnDispose);
        }


        public void BeginMessage(MessageType messageType)
        {
            _messageType = messageType;
            switch (messageType)
            {
                default:
                    throw new ArgumentOutOfRangeException("messageType");

                case MessageType.Error:
                    _packetType = ErrorMessage;
                    break;
                case MessageType.Request:
                    _packetType = RequestMessage;
                    break;
                case MessageType.Response:
                    _packetType = ResponseMessage;
                    break;
            }

            _writeBuffer.WriteRaw(PacketHeader);
        }

        public void SetMethod(string method)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentNullException("method");
            }
            if (_messageType != MessageType.Request)
            {
                throw new InvalidOperationException("Method is only allow in request messages.");
            }

            _writeBuffer.Write(method);
        }

        public void BeginHeaders(int headerCount)
        {
            if (headerCount < 0)
            {
                throw new ArgumentOutOfRangeException("headerCount");
            }

            if (_packetType == ErrorMessage)
            {
                throw new InvalidOperationException("Headers not supported for error message.");
            }

            _headerCount = headerCount;

            if (headerCount == 0)
            {
                return;
            }

            StartBuffer();

            _writeBuffer.Write(headerCount);
        }

        public void WriteHeader(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key", "Parameter key cannot be null or empty.");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (_packetType == ErrorMessage)
            {
                throw new InvalidOperationException("Headers not supported for error message.");
            }

            _headersWritten++;
            _writeBuffer.Write(key);
            _writeBuffer.Write(value);
        }

        public void EndHeaders()
        {
            if (_packetType == ErrorMessage)
            {
                throw new InvalidOperationException("Headers not supported for error message.");
            }

            if (_headersWritten != _headerCount)
            {
                throw new InvalidOperationException(
                    string.Format("Expected {0} headers to be written, got {1} instead.", _headerCount, _headersWritten));
            }

            if (_headerCount > 0)
            {
                _packetType |= MessageContainsHeaders;
            }
        }

        public void BeginContent()
        {
            _outputWriter.WriteRaw(_packetType);

            FlushBufferedWriter();

            StartBuffer();
        }

        public void EndContent()
        {
            FlushBufferedWriter();
        }

        public void BeginArray(int count)
        {
            WriteComplexTypeHeader(ContentType.Array, count);
        }

        public void BeginItem()
        {
            _paramDepth++;
        }

        public void BeginStruct(int count)
        {
            WriteComplexTypeHeader(ContentType.Struct, count);
        }

        public void EndArray()
        {
        }

        public void EndItem()
        {
            _paramDepth--;
        }

        public void EndStruct()
        {
        }

        public void WriteBase64String(string base64String)
        {
            _writeBuffer.Write(ContentType.Base64);
            _writeBuffer.Write(base64String);
        }

        public void WriteBooleanValue(bool value)
        {
            _writeBuffer.Write(ContentType.Boolean);
            _writeBuffer.Write(value);
        }

        public void WriteDoubleValue(double value)
        {
            _writeBuffer.Write(ContentType.Float);
            _writeBuffer.Write(value);
        }

        public void WriteInt32Value(int value)
        {
            _writeBuffer.Write(ContentType.Int);
            _writeBuffer.Write(value);
        }

        public void WritePropertyName(string name)
        {
            _writeBuffer.Write(name);
        }

        public void WriteStringValue(string value)
        {
            _writeBuffer.Write(ContentType.String);
            _writeBuffer.Write(value);
        }

        public void EndMessage()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            using (_outputWriter) { }
            using (_writeBuffer) { }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public override string ToString()
        {
            return _outputWriter.ToString();
        }

        private void WriteComplexTypeHeader(ContentType contentType, int count)
        {
            if (_messageType != MessageType.Request || _paramDepth > 0)
            {
                _writeBuffer.Write(contentType);
            }

            _writeBuffer.Write(count);
        }

        private void StartBuffer()
        {
            _writeBuffer = new HmBinaryStreamWriter(new MemoryStream(), true);
        }

        private void FlushBufferedWriter()
        {
            var bufferedWriter = _writeBuffer;
            if (bufferedWriter == _outputWriter)
            {
                return;
            }
            _writeBuffer = _outputWriter;

            using (bufferedWriter)
            {
                _writeBuffer.Write((int)bufferedWriter.Length);
                bufferedWriter.WriteTo(_writeBuffer);
            }
        }
    }

}