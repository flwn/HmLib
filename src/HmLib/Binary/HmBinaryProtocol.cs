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

        public void ReadRequest(HmBinaryReader input, IMessageBuilder messageBuilder)
        {
            var streamReader = (IHmStreamReader)input;
            var reader = (IMessageReader)input;

            if (!reader.Read())
            {
                throw new ProtocolException("Packet Header not recognized.");
            }

            if (reader.MessageType != MessageType.Request)
            {
                throw new ProtocolException("Expected request.");
            }

            messageBuilder.BeginMessage(MessageType.Request);
            reader.Read();
            if (reader.MessagePart == HmMessagePart.Headers)
            {
                ReadHeaders(reader, messageBuilder);
            }

            messageBuilder.BeginContent();
            reader.Read();
            messageBuilder.SetMethod(reader.Value);

            ReadArray(streamReader, messageBuilder);
            messageBuilder.EndContent();
            messageBuilder.EndMessage();

            if (input.BytesRead > int.MaxValue)
            {
                throw new ProtocolException("The response message is too large to handle.");
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
            var binaryReader = new HmBinaryReader(inputStream);
            var streamReader = (IHmStreamReader)binaryReader;
            var reader = (IMessageReader)binaryReader;

            if (!reader.Read())
            {
                throw new ProtocolException("Packet Header not recognized.");
            }
            if (reader.MessageType == MessageType.Request)
            {
                throw new ProtocolException("Expected response.");
            }

            output.BeginMessage(reader.MessageType);
            reader.Read();
            if (reader.MessagePart == HmMessagePart.Headers)
            {
                ReadHeaders(reader, output);
            }

            output.BeginContent();
            if (reader.MessagePart != HmMessagePart.EndOfFile)
            {
                ReadResponse(streamReader, output);
            }
            output.EndContent();

            if (binaryReader.BytesRead > int.MaxValue)
            {
                throw new ProtocolException("The response message is too large to handle.");
            }

            return;

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

        private void ReadHeaders(IMessageReader reader, IMessageBuilder output)
        {
            var headerCount = reader.HeaderCount;
            output.BeginHeaders(headerCount);

            for (; headerCount > 0; headerCount--)
            {
                reader.Read();
                output.WriteHeader(reader.Key, reader.Value);
            }
            output.EndHeaders();
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