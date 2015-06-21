using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HmLib.Binary
{
    using Serialization;

    public class HmBinaryMessageReader : IMessageReader
    {
        private static readonly byte[] PacketHeader = Encoding.ASCII.GetBytes("Bin");

        private const byte ErrorMessage = 0xff;
        private const byte RequestMessage = 0x00;
        private const byte ResponseMessage = 0x01;
        private const byte MessageContainsHeaders = 0x40;

        private readonly HmBinaryStreamReader _stream;

        private enum BodyState
        {
            RequestMethod,
            RequestParameters,
            Content,
        }
        private BodyState _bodyState;

        public HmBinaryMessageReader(Stream input)
        {
            _reader = InitialReader;
            _stream = new HmBinaryStreamReader(input);
        }

        public HmMessagePart MessagePart { get; private set; }
        public MessageType MessageType { get; private set; }

        private bool _containsHeaders;

        private int _expectedHeaderLength;
        private int _headersRead = 0;
        private int _headerOffset;

        private int _expectedBodyLength;
        private int _bodyOffset;

        public int CollectionCount { get; private set; }

        public string PropertyName { get; private set; }
        public string StringValue { get; private set; }

        public int IntValue { get; private set; }
        public double DoubleValue { get; private set; }
        public bool BooleanValue { get; private set; }

        public ContentType? ValueType { get; private set; }

        private Func<bool> _reader;

        public bool Read()
        {
            return _reader();
        }

        private bool InitialReader()
        {
            if (ReadPacketType())
            {
                MessagePart = HmMessagePart.Message;
                _reader = ReadInteractive;
                return true;
            }

            _reader = ErrorReader;
            return false;
        }

        private bool EndOfFileReader()
        {
            return false;
        }
        private bool ErrorReader()
        {
            return false;
        }

        private Stack<Tuple<bool, int>> _collectionDepth = new Stack<Tuple<bool, int>>();
        private bool _readKeyValuePairs;
        private int _itemsToReadInCurrentCollection;
        private void BeginCollection()
        {
            //store current state
            _collectionDepth.Push(Tuple.Create(_readKeyValuePairs, _itemsToReadInCurrentCollection));

            _itemsToReadInCurrentCollection = CollectionCount;
            _readKeyValuePairs = ValueType == ContentType.Struct;
        }
        private void EndItem()
        {
            _itemsToReadInCurrentCollection--;
            if (_itemsToReadInCurrentCollection == 0)
            {
                var tmp = _collectionDepth.Pop();
                _readKeyValuePairs = tmp.Item1;
                _itemsToReadInCurrentCollection = tmp.Item2;
                EndItem();
            }
        }

        private bool ReadInteractive()
        {
            switch (MessagePart)
            {

                case HmMessagePart.Body:
                    var bodyRead = (int)_stream.BytesRead - _bodyOffset;
                    if (bodyRead >= _expectedBodyLength)
                    {
                        _reader = ErrorReader;
                        throw new InvalidOperationException("Read more than expected.");
                    }

                    ReadBody();

                    bodyRead = (int)_stream.BytesRead - _bodyOffset;
                    if (bodyRead < _expectedBodyLength)
                    {
                        return true;
                    }

                    MoveToEof();
                    return false;
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
                    if (_headersRead < CollectionCount)
                    {
                        ReadHeader();

                        if (_headersRead < CollectionCount)
                        {
                            return true;
                        }
                    }

                    var actualBytesRead = _stream.BytesRead - _headerOffset;
                    if (_expectedHeaderLength != actualBytesRead)
                    {
                        _reader = ErrorReader;
                        throw new ProtocolException(string.Format("Expected a header of length {0} bytes, instead read {1} bytes.", _expectedHeaderLength, actualBytesRead));
                    }

                    MoveToContent();

                    return true;
            }
            return false;
        }

        private void ReadHeader()
        {
            _headersRead++;
            PropertyName = _stream.ReadString();
            ValueType = ContentType.String;
            StringValue = _stream.ReadString();
        }

        private void ReadBody()
        {
            //todo: implement yield return...
            if (_bodyState == BodyState.Content)
            {
                if (_readKeyValuePairs)
                {
                    PropertyName = _stream.ReadString();
                }

                ValueType = _stream.ReadContentType();

                if (ValueType == ContentType.Array || ValueType == ContentType.Struct)
                {
                    CollectionCount = _stream.ReadInt32();
                    BeginCollection();
                    return;
                }

                if (ValueType == ContentType.String || ValueType == ContentType.Base64)
                {
                    StringValue = _stream.ReadString();
                }
                else if (ValueType == ContentType.Int)
                {
                    IntValue = _stream.ReadInt32();
                }
                else if (ValueType == ContentType.Float)
                {
                    DoubleValue = _stream.ReadDouble();
                }
                else if (ValueType == ContentType.Boolean)
                {
                    BooleanValue = _stream.ReadBoolean();
                }
                else
                {
                    throw new NotImplementedException();
                }

                EndItem();
                return;
            }

            if (_bodyState == BodyState.RequestMethod)
            {
                ValueType = ContentType.String;
                StringValue = _stream.ReadString();
                _bodyState = BodyState.RequestParameters;
                return;
            }

            //params
            ValueType = ContentType.Array;
            CollectionCount = _stream.ReadInt32();
            _bodyState = BodyState.Content;
            BeginCollection();
        }


        private bool ReadPacketType()
        {
            var header = _stream.ReadBytes(3);
            if (!PacketHeader.SequenceEqual(header))
            {
                return false;
            }

            var packetType = _stream.ReadByte();
            switch (packetType)
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
            _reader = EndOfFileReader;
            MessagePart = HmMessagePart.EndOfFile;

            if (_stream.BytesRead - _bodyOffset != _expectedBodyLength)
            {
                throw new ProtocolException("The response is incomplete or corrupted.");
            }
        }

        private void MoveToHeaders()
        {
            MessagePart = HmMessagePart.Headers;

            _expectedHeaderLength = _stream.ReadInt32();
            _headerOffset = (int)_stream.BytesRead;
            CollectionCount = _stream.ReadInt32();
        }

        private void MoveToContent()
        {
            _expectedBodyLength = _stream.ReadInt32();
            _bodyOffset = (int)_stream.BytesRead;

            if (_expectedBodyLength == 0)
            {
                MoveToEof();
                return;
            }
            MessagePart = HmMessagePart.Body;

            if (MessageType == MessageType.Request)
            {
                _bodyState = BodyState.RequestMethod;
            }
            else
            {
                _bodyState = BodyState.Content;
            }
        }


    }
}