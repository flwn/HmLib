using System;
using System.IO;
using System.Text;

namespace HmLib.Binary
{
    using Serialization;
    using System.Linq;

    public enum ReadState
    {
        Initial,
        Error,
        Interactive,
        EndOfFile,
        Closed,
    }


    public class HmBinaryReader : IHmStreamReader, IMessageReader
    {
        private static readonly byte[] PacketHeader = Encoding.ASCII.GetBytes("Bin");
        private static readonly Encoding Encoding = Encoding.ASCII;

        private const byte ErrorMessage = 0xff;
        private const byte RequestMessage = 0x00;
        private const byte ResponseMessage = 0x01;
        private const byte MessageContainsHeaders = 0x40;

        private long _bytesReadTotal = 0L;
        private Stream _input;
        public ReadState ReadState { get; private set; }

        private enum BodyState
        {
            RequestMethod,
            RequestParameters,
        }
        private BodyState _bodyState;

        public HmBinaryReader(Stream input, bool leaveOpen = true)
        {
            _input = input;
        }

        public HmMessagePart MessagePart { get; private set; }
        public MessageType MessageType { get; private set; }

        private bool _containsHeaders;

        private byte _packetType;

        private int _expectedHeaderLength;
        private int _headersRead = 0;
        private int _headerOffset;

        private int _expectedBodyLength;
        private int _bodyOffset;


        public int HeaderCount { get; private set; }
        public string Key { get; private set; }
        public string Value { get; private set; }

        public ContentType? ValueType { get; private set; }

        public bool Read()
        {
            switch (ReadState)
            {
                case ReadState.Initial:
                    if (ReadPacketType())
                    {
                        MessagePart = HmMessagePart.Message;
                        ReadState = ReadState.Interactive;
                        return true;
                    }
                    break;
                case ReadState.Interactive:
                    Key = Value = null;
                    return ReadInteractive();
                case ReadState.EndOfFile:
                    return false;
                case ReadState.Error:
                    return false;
                default:
                    break;
            }

            ReadState = ReadState.Error;
            return false;
        }



        private bool ReadInteractive()
        {
            switch (MessagePart)
            {
                case HmMessagePart.Message:
                    if (_containsHeaders)
                    {
                        MoveToHeaders();
                    }
                    else
                    {
                        MoveToContent();
                    }
                    return true;

                case HmMessagePart.Headers:
                    if (_headersRead < HeaderCount)
                    {
                        ReadHeader();

                        if (_headersRead < HeaderCount)
                        {
                            return true;
                        }
                    }

                    var actualBytesRead = BytesRead - _headerOffset;
                    if (_expectedHeaderLength != actualBytesRead)
                    {
                        throw new ProtocolException(string.Format("Expected a header of length {0} bytes, instead read {1} bytes.", _expectedHeaderLength, actualBytesRead));
                    }

                    MoveToContent();

                    return true;

                case HmMessagePart.Body:

                    var bodyRead = (int)BytesRead - _bodyOffset;

                    if (bodyRead == _expectedBodyLength)
                    {
                        MoveToEof();
                        return false;
                    }
                    switch (_bodyState)
                    {
                        case BodyState.RequestMethod:
                            Value = ReadString();
                            _bodyState = BodyState.RequestParameters;
                            break;
                        case BodyState.RequestParameters:

                            break;
                    }
                    break;
            }
            return false;
        }

        private void ReadHeader()
        {
            _headersRead++;
            Key = ReadString();
            Value = ReadString();
        }

        private void ReadBody()
        {

        }

        private bool ReadPacketType()
        {
            var header = ReadBytes(3);
            if (!PacketHeader.SequenceEqual(header))
            {
                return false;
            }

            _packetType = ReadByte();
            switch (_packetType)
            {
                case ErrorMessage:
                    MessageType = MessageType.Error;
                    return true;
                case ResponseMessage:
                    MessageType = MessageType.Response;
                    return true;
                case RequestMessage:
                    MessageType = MessageType.Request;
                    return true;
                case RequestMessage | MessageContainsHeaders:
                    MessageType = MessageType.Request;
                    _containsHeaders = true;
                    return true;
                case ResponseMessage | MessageContainsHeaders:
                    MessageType = MessageType.Response;
                    _containsHeaders = true;
                    return true;
                default:
                    return false;
            }
        }

        private void MoveToEof()
        {
            ReadState = ReadState.EndOfFile;
            MessagePart = HmMessagePart.EndOfFile;

            if (BytesRead - _bodyOffset != _expectedBodyLength)
            {
                throw new ProtocolException("The response is incomplete or corrupted.");
            }
        }

        private void MoveToHeaders()
        {
            MessagePart = HmMessagePart.Headers;

            _expectedHeaderLength = ReadInt32();
            _headerOffset = (int)BytesRead;
            HeaderCount = ReadInt32();
        }

        private void MoveToContent()
        {
            _expectedBodyLength = ReadInt32();
            _bodyOffset = (int)BytesRead;

            if (_expectedBodyLength > 0)
            {
                MessagePart = HmMessagePart.Body;

                if (MessageType == MessageType.Request)
                {
                    _bodyState = BodyState.RequestMethod;
                }
            }
            else
            {
                MoveToEof();
            }
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
            ValueType = ContentType.String;
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

            if (count == 0)
            {
                return buffer;
            }

            var bytesRead = 0;
            do
            {
                var read = _input.Read(buffer, bytesRead, count - bytesRead);

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

    }
}