using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace HmLib.Binary
{
    using Serialization;

    public class HmBinaryProtocol : IProtocol
    {
        private static readonly byte[] PacketHeader = Encoding.ASCII.GetBytes("Bin");

        private enum PacketType : byte
        {
            BinaryRequest = 0x00,
            BinaryResponse = 0x01,
            BinaryRequestHeader = 0x40,
            BinaryResponseHeader = 0x41,
            ErrorResponse = 0xff,
        }

        private readonly HmSerializer _bodySerializer = new HmSerializer();

        public HmBinaryProtocol()
        {
        }

        public void WriteRequest(Stream outputStream, Request request)
        {
            var messageBuilder = CreateMessageBuilder(outputStream);

            messageBuilder.BeginMessage(MessageType.Request);

            SerializeHeaders(messageBuilder, request.Headers);

            SerializeContent(messageBuilder, request.Method, request.Parameters);

            messageBuilder.EndMessage();
        }

        public void ReadRequest(Stream inputStream, IMessageBuilder messageBuilder)
        {
            var reader = new HmBinaryReader(inputStream);

            var responseHeader = reader.ReadBytes(3);

            if (!responseHeader.SequenceEqual(PacketHeader))
            {
                throw new ProtocolException("Packet Header not recognized.");
            }

            var responseType = (PacketType)reader.ReadByte();

            messageBuilder.BeginMessage(MessageType.Request);

            switch (responseType)
            {
                case PacketType.BinaryRequestHeader:
                    ReadHeaders(reader, messageBuilder);
                    goto case PacketType.BinaryRequest;

                case PacketType.BinaryRequest:

                    var responseLength = reader.ReadInt32();
                    var contentOffset = reader.BytesRead;
                    messageBuilder.BeginContent();
                    var method = reader.ReadString();
                    messageBuilder.SetMethod(method);

                    ReadArray(reader, messageBuilder);
                    messageBuilder.EndContent();
                    messageBuilder.EndMessage();
                    var bytesRead = reader.BytesRead - contentOffset;

                    if (reader.BytesRead > int.MaxValue)
                    {
                        throw new ProtocolException("The response message is too large to handle.");
                    }

                    if (bytesRead != responseLength)
                    {
                        throw new ProtocolException("The response is incomplete or corrupted.");
                    }
                    break;
                default:
                    Debugger.Break();
                    throw new ProtocolException("Request type not recognized.");
            }

            messageBuilder.EndMessage();
        }

        public void WriteErrorResponse(Stream outputStream, string errorMessage)
        {
            var messageBuilder = CreateMessageBuilder(outputStream);

            messageBuilder.BeginMessage(MessageType.Error);

            var response = new Dictionary<string, object> { { "faultCode", -10 }, { "faultString", errorMessage } };
            messageBuilder.BeginContent();
            _bodySerializer.Serialize(messageBuilder, response);
            messageBuilder.EndContent();

            messageBuilder.EndMessage();
        }

        public void WriteResponse(Stream outputStream, object response)
        {
            var messageBuilder = CreateMessageBuilder(outputStream);

            messageBuilder.BeginMessage(MessageType.Response);
            messageBuilder.BeginContent();
            _bodySerializer.Serialize(messageBuilder, response);
            messageBuilder.EndContent();
            messageBuilder.EndMessage();
        }

        public void ReadResponse(Stream inputStream, IMessageBuilder output)
        {
            var reader = new HmBinaryReader(inputStream);

            var responseHeader = reader.ReadBytes(3);

            if (!responseHeader.SequenceEqual(PacketHeader))
            {
                throw new ProtocolException("Packet Header not recognized.");
            }

            var responseType = (PacketType)reader.ReadByte();
            if (responseType == PacketType.ErrorResponse)
            {
                output.BeginMessage(MessageType.Error);
            }
            else
            {
                output.BeginMessage(MessageType.Response);
            }

            switch (responseType)
            {
                default:
                    Debugger.Break();
                    throw new ProtocolException();

                case PacketType.BinaryResponseHeader:
                    ReadHeaders(reader, output);
                    goto case PacketType.BinaryResponse;

                case PacketType.ErrorResponse:
                case PacketType.BinaryResponse:

                    var responseLength = reader.ReadInt32();
                    var contentOffset = reader.BytesRead;

                    output.BeginContent();
                    if (responseLength > 0)
                    {
                        ReadResponse(reader, output);
                    }
                    output.EndContent();
                    var bytesRead = reader.BytesRead - contentOffset;

                    if (reader.BytesRead > int.MaxValue)
                    {
                        throw new ProtocolException("The response message is too large to handle.");
                    }

                    if (bytesRead != responseLength)
                    {
                        throw new ProtocolException("The response is incomplete or corrupted.");
                    }


                    return;

            }
        }



        private void SerializeHeaders(IMessageBuilder builder, IDictionary<string, string> headerDictionary)
        {
            builder.BeginHeaders(headerDictionary.Count);
            foreach (var header in headerDictionary)
            {
                builder.WriteHeader(header.Key, header.Value);
            }
            builder.EndHeaders();
        }

        private void SerializeContent(IMessageBuilder builder, string methodName, ICollection<object> parameters)
        {
            builder.BeginContent();
            builder.SetMethod(methodName);

            _bodySerializer.Serialize(builder, parameters);

            builder.EndContent();
        }

        private void ReadHeaders(HmBinaryReader reader, IMessageBuilder output)
        {
            var headerLenght = reader.ReadInt32();
            var headerOffset = reader.BytesRead;


            var headerCount = reader.ReadInt32();
            output.BeginHeaders(headerCount);

            for (; headerCount > 0; headerCount--)
            {
                var key = reader.ReadString();
                var value = reader.ReadString();

                output.WriteHeader(key, value);
            }
            output.EndHeaders();

            var actualBytesRead = reader.BytesRead - headerOffset;
            if (actualBytesRead != headerLenght)
            {
                throw new ProtocolException(string.Format("Expected a header of length {0} bytes, instead read {1} bytes.", headerLenght, actualBytesRead));
            }
        }



        private void ReadResponse(IHmStreamReader reader, IObjectBuilder builder)
        {
            var type = reader.ReadContentType();

            switch (type)
            {
                case ContentType.Array:
                    ReadArray(reader, builder);
                    break;
                case ContentType.Struct:
                    ReadStruct(reader, builder);
                    break;
                case ContentType.Int:
                    builder.WriteInt32Value(reader.ReadInt32());
                    break;
                case ContentType.Boolean:
                    builder.WriteBooleanValue(reader.ReadBoolean());
                    break;
                case ContentType.String:
                    builder.WriteStringValue(reader.ReadString());
                    break;
                case ContentType.Float:
                    builder.WriteDoubleValue(reader.ReadDouble());
                    break;
                case ContentType.Base64:
                    builder.WriteBase64String(reader.ReadString());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ReadStruct(IHmStreamReader reader, IObjectBuilder builder)
        {
            var elementCount = reader.ReadInt32();
            builder.BeginStruct(elementCount);

            for (; elementCount > 0; elementCount--)
            {
                builder.BeginItem();

                var propertyName = reader.ReadString();
                builder.WritePropertyName(propertyName);

                ReadResponse(reader, builder);

                builder.EndItem();
            }

            builder.EndStruct();
        }

        private void ReadArray(IHmStreamReader reader, IObjectBuilder builder)
        {
            var itemCount = reader.ReadInt32();

            builder.BeginArray(itemCount);
            for (; itemCount > 0; itemCount--)
            {
                builder.BeginItem();
                ReadResponse(reader, builder);
                builder.EndItem();
            }
            builder.EndArray();
        }

        private static IMessageBuilder CreateMessageBuilder(Stream output)
        {
            return new HmBinaryMessageWriter(output);
        }
    }
}