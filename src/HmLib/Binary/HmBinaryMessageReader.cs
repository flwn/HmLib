using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HmLib.Binary
{
    using Abstractions;

    public class HmBinaryMessageReader : IMessageReader
    {
        private readonly HmBinaryStreamReader _stream;

        private bool _containsHeaders;

        private int _expectedHeaderLength;
        private int _headersToRead = 0;
        private int _headerOffset;

        private int _expectedBodyEnd;

        private Token _currentToken;

        private Exception _error;

        private IEnumerator<Token> _tokenReader;

        public HmBinaryMessageReader(Stream input)
        {
            _reader = InitialReader;
            _stream = new HmBinaryStreamReader(input);
        }

        public ReadState ReadState { get; private set; }
        public MessageType MessageType { get; private set; }


        public int ItemCount => _currentToken.ItemCount;

        public string PropertyName => _currentToken.PropertyName;
        public string StringValue => _currentToken.String;

        public int IntValue => _currentToken.Int;
        public double DoubleValue => _currentToken.Double;
        public bool BooleanValue => _currentToken.Boolean;

        public ContentType? ValueType => _currentToken.Type;

        private Func<bool> _reader;

        public bool Read() => _reader();

        private bool InitialReader()
        {
            if (ReadPacketType())
            {
                ReadState = ReadState.Message;
                _reader = ReadInteractive;
                return true;
            }

            return false;
        }

        private bool EndOfFileReader() => false;
        private bool ErrorReader()
        {
            throw _error;
        }

        private Stack<Tuple<bool, int>> _collectionDepth = new Stack<Tuple<bool, int>>();
        private bool _readKeyValuePairs;
        private int _itemsToReadInCurrentCollection;
        private void BeginCollection()
        {
            //store current state
            _collectionDepth.Push(Tuple.Create(_readKeyValuePairs, _itemsToReadInCurrentCollection));

            _itemsToReadInCurrentCollection = ItemCount;
            _readKeyValuePairs = ValueType == ContentType.Struct;
        }
        private void CompleteCollectionItem()
        {
            _itemsToReadInCurrentCollection--;
            if (_itemsToReadInCurrentCollection == 0)
            {
                var tmp = _collectionDepth.Pop();
                _readKeyValuePairs = tmp.Item1;
                _itemsToReadInCurrentCollection = tmp.Item2;
                CompleteCollectionItem();
            }
        }

        private bool ReadInteractive()
        {
            switch (ReadState)
            {
                case ReadState.Body:
                    if (_stream.BytesRead < _expectedBodyEnd)
                    {
                        ReadBody();
                    }
                    else
                    {
                        MoveToEof();
                    }

                    return true;

                case ReadState.Message:
                    if (_containsHeaders)
                    {
                        MoveToHeaders();
                    }
                    else
                    {
                        MoveToContent();
                    }
                    return true;

                case ReadState.Headers:
                    if (_headersToRead > 0)
                    {
                        ReadHeader();

                        return true;
                    }

                    var actualBytesRead = _stream.BytesRead - _headerOffset;
                    if (_expectedHeaderLength != actualBytesRead)
                    {
                        var error = new ProtocolException($"Expected a header of length {_expectedHeaderLength} bytes, instead read {actualBytesRead} bytes.");
                        SetError(error);
                        return false;
                    }

                    MoveToContent();

                    return true;
            }
            return false;
        }

        private void ReadHeader()
        {
            _headersToRead--;
            var key = _stream.ReadString();
            var value = _stream.ReadString();
            _currentToken = new Token
            {
                PropertyName = key,
                Type = ContentType.String,
                String = value
            };
        }

        private IEnumerable<Token> ReadTokens()
        {
            if (MessageType == MessageType.Request)
            {
                yield return new Token
                {
                    Type = ContentType.Struct,
                    ItemCount = 2
                };

                //request method
                yield return new Token
                {
                    PropertyName = "method",
                    Type = ContentType.String,
                    String = _stream.ReadString()
                };

                //request method
                yield return new Token
                {
                    PropertyName = "parameters",
                    Type = ContentType.Array,
                    ItemCount = _stream.ReadInt32()
                };
            }

            while (_expectedBodyEnd > _stream.BytesRead)
            {
                var propertyName = _readKeyValuePairs ? _stream.ReadString() : null;

                var contentType = _stream.ReadContentType();

                var token = new Token
                {
                    Type = contentType,
                    PropertyName = propertyName,
                };

                switch (contentType)
                {
                    case ContentType.Array:
                    case ContentType.Struct:
                        token.ItemCount = _stream.ReadInt32();
                        break;
                    case ContentType.Base64:
                    case ContentType.String:
                        token.String = _stream.ReadString();
                        break;
                    case ContentType.Boolean:
                        token.Boolean = _stream.ReadBoolean();
                        break;
                    case ContentType.Float:
                        token.Double = _stream.ReadDouble();
                        break;
                    case ContentType.Int:
                        token.Int = _stream.ReadInt32();
                        break;

                }

                yield return token;
            }
        }

        private void ReadBody()
        {
            if (!_tokenReader.MoveNext())
            {
                MoveToEof();
                return;
            }

            _currentToken = _tokenReader.Current;

            if (ValueType == ContentType.Array || ValueType == ContentType.Struct)
            {
                BeginCollection();
                return;
            }

            CompleteCollectionItem();
        }


        private bool ReadPacketType()
        {
            var header = _stream.ReadBytes(3);
            if (!Packet.PacketHeader.SequenceEqual(header))
            {
                var error = new ProtocolException("Packet header is not recognized");
                SetError(error);
                return false;
            }

            var packetType = _stream.ReadByte();
            switch (packetType)
            {
                case Packet.ErrorMessage:
                    MessageType = MessageType.Error;
                    return true;
                case Packet.ResponseMessage:
                    MessageType = MessageType.Response;
                    return true;
                case Packet.RequestMessage:
                    MessageType = MessageType.Request;
                    return true;
                case Packet.RequestMessageWithHeaders:
                    MessageType = MessageType.Request;
                    _containsHeaders = true;
                    return true;
                case Packet.ResponseMessageWithHeaders:
                    MessageType = MessageType.Response;
                    _containsHeaders = true;
                    return true;
                default:
                    var error = new ProtocolException($"Packet type {packetType:X2} is not recognized");
                    SetError(error);
                    return false;
            }
        }

        private void MoveToEof()
        {
            if (ReadState == ReadState.Error)
            {
                return;
            }

            _reader = EndOfFileReader;
            ReadState = ReadState.EndOfFile;

            if (_stream.BytesRead != _expectedBodyEnd)
            {
                var error = new ProtocolException($"The response is incomplete or corrupted. Expected {_expectedBodyEnd} bytes, read {_stream.BytesRead} bytes.");
                SetError(error);
            }
        }

        private void MoveToHeaders()
        {
            ReadState = ReadState.Headers;

            _expectedHeaderLength = _stream.ReadInt32();
            _headerOffset = (int)_stream.BytesRead;
            _headersToRead = _stream.ReadInt32();
            _currentToken = new Token
            {
                ItemCount = _headersToRead
            };
        }

        private void MoveToContent()
        {
            var expectedBodyLength = _stream.ReadInt32();
            var bodyOffset = (int)_stream.BytesRead;

            _expectedBodyEnd = bodyOffset + expectedBodyLength;

            if (expectedBodyLength == 0)
            {
                MoveToEof();
                return;
            }

            _tokenReader = ReadTokens().GetEnumerator();

            ReadState = ReadState.Body;
        }

        private void SetError(Exception error)
        {
            _error = error;
            ReadState = ReadState.Error;
            _reader = ErrorReader;
        }

        private struct Token
        {

            public ContentType Type;

            public int ItemCount;

            public string PropertyName;

            public string String;

            public int Int;

            public double Double;

            public bool Boolean;
        }
    }
}