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

        public HmBinaryMessageReader(Stream input)
        {
            _reader = InitialReader;
            _stream = new HmBinaryStreamReader(input);
        }

        public ReadState ReadState { get; private set; }
        public MessageType MessageType { get; private set; }

        private bool _containsHeaders;

        private int _expectedHeaderLength;
        private int _headersRead = 0;
        private int _headerOffset;

        private int _expectedBodyEnd;

        public int ItemCount { get; private set; }

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
                ReadState = ReadState.Message;
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

            _itemsToReadInCurrentCollection = ItemCount;
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
                    if (_headersRead < ItemCount)
                    {
                        ReadHeader();

                        if (_headersRead < ItemCount)
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

        private IEnumerable<ContentType> ReadValueType()
        {
            if (MessageType == MessageType.Request)
            {
                //request method
                yield return ContentType.String;

                //request params
                yield return ContentType.Array;
            }

            while (_expectedBodyEnd > _stream.BytesRead)
            {
                var contentType = _stream.ReadContentType();
                yield return contentType;
            }
        }

        private void ReadBody()
        {
            if (_readKeyValuePairs)
            {
                PropertyName = _stream.ReadString();
            }

            if (!_typeReader.MoveNext())
            {
                MoveToEof();
                return;
            }

            ValueType = _typeReader.Current;

            if (ValueType == ContentType.Array || ValueType == ContentType.Struct)
            {
                ItemCount = _stream.ReadInt32();
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
        }


        private bool ReadPacketType()
        {
            var header = _stream.ReadBytes(3);
            if (!Packet.PacketHeader.SequenceEqual(header))
            {
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
                    return false;
            }
        }

        private void MoveToEof()
        {
            _reader = EndOfFileReader;
            ReadState = ReadState.EndOfFile;

            if (_stream.BytesRead != _expectedBodyEnd)
            {
                throw new ProtocolException(string.Format("The response is incomplete or corrupted. Expected {0} bytes, read {1} bytes.", _expectedBodyEnd, _stream.BytesRead));
            }
        }

        private void MoveToHeaders()
        {
            ReadState = ReadState.Headers;

            _expectedHeaderLength = _stream.ReadInt32();
            _headerOffset = (int)_stream.BytesRead;
            ItemCount = _stream.ReadInt32();
        }

        private IEnumerator<ContentType> _typeReader;

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

            _typeReader = ReadValueType().GetEnumerator();

            ReadState = ReadState.Body;
        }


    }
}