using System;
using System.Collections.Generic;

namespace HmLib
{
    using Serialization;

    public class RequestResponseProtocol : IProtocol
    {
        private readonly HmSerializer _bodySerializer = new HmSerializer();

        public void WriteRequest(IMessageBuilder output, Request request)
        {
            var requestReader = new MessageReader(request);
            ConvertRequest(requestReader, output);
        }

        public void ReadRequest(IMessageReader input, IMessageBuilder output)
        {
            ConvertRequest(input, output);
        }

        private void ConvertRequest(IMessageReader input, IMessageBuilder output)
        {
            if (!input.Read())
            {
                throw new ProtocolException("Packet Header not recognized.");
            }

            if (input.MessageType != MessageType.Request)
            {
                throw new ProtocolException("Expected request.");
            }

            output.BeginMessage(input.MessageType);

            input.Read();
            if (input.MessagePart == HmMessagePart.Headers)
            {
                ConvertHeaders(input, output);
                input.Read();
            }

            output.BeginContent();

            input.Read();
            output.SetMethod(input.StringValue);

            input.Read();
            ReadArrayContent(input, output);

            output.EndContent();
            output.EndMessage();

            input.Read();
            if (input.MessagePart != HmMessagePart.EndOfFile)
            {
                throw new ProtocolException("Expected EndOfFile");
            }
        }



        public void WriteErrorResponse(IMessageBuilder output, string errorMessage)
        {
            output.BeginMessage(MessageType.Error);

            var response = new Dictionary<string, object> { { "faultCode", -10 }, { "faultString", errorMessage } };
            output.BeginContent();
            _bodySerializer.Serialize(output, response);
            output.EndContent();

            output.EndMessage();
        }

        public void WriteResponse(IMessageBuilder output, object response)
        {
            output.BeginMessage(MessageType.Response);
            output.BeginContent();
            _bodySerializer.Serialize(output, response);
            output.EndContent();
            output.EndMessage();
        }

        public void ReadResponse(IMessageReader input, IMessageBuilder output)
        {
            if (!input.Read())
            {
                throw new ProtocolException("Packet Header not recognized.");
            }
            if (input.MessageType == MessageType.Request)
            {
                throw new ProtocolException("Expected response.");
            }

            output.BeginMessage(input.MessageType);
            input.Read();
            if (input.MessagePart == HmMessagePart.Headers)
            {
                ConvertHeaders(input, output);
            }

            output.BeginContent();
            if (input.MessagePart != HmMessagePart.EndOfFile)
            {
                input.Read();
                ReadValue(input, output);
            }
            output.EndContent();
            output.EndMessage();

            input.Read();
            if (input.MessagePart != HmMessagePart.EndOfFile)
            {
                throw new ProtocolException("Expected EndOfFile");
            }

            return;

        }


        private void ConvertHeaders(IMessageReader input, IMessageBuilder output)
        {
            var headerCount = input.CollectionCount;
            output.BeginHeaders(headerCount);

            for (; headerCount > 0; headerCount--)
            {
                input.Read();
                output.WriteHeader(input.PropertyName, input.StringValue);
            }
            output.EndHeaders();
        }



        private void ReadValue(IMessageReader reader, IObjectBuilder builder)
        {
            switch (reader.ValueType)
            {
                case ContentType.Array:
                    ReadArrayContent(reader, builder);
                    break;
                case ContentType.Struct:
                    ReadStructContent(reader, builder);
                    break;
                case ContentType.Int:
                    builder.WriteInt32Value(reader.IntValue);
                    break;
                case ContentType.Boolean:
                    builder.WriteBooleanValue(reader.BooleanValue);
                    break;
                case ContentType.String:
                    builder.WriteStringValue(reader.StringValue);
                    break;
                case ContentType.Float:
                    builder.WriteDoubleValue(reader.DoubleValue);
                    break;
                case ContentType.Base64:
                    builder.WriteBase64String(reader.StringValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ReadStructContent(IMessageReader reader, IObjectBuilder builder)
        {
            var elementCount = reader.CollectionCount;
            builder.BeginStruct(reader.CollectionCount);

            for (; elementCount > 0; elementCount--)
            {
                builder.BeginItem();
                reader.Read();
                builder.WritePropertyName(reader.PropertyName);

                ReadValue(reader, builder);

                builder.EndItem();
            }

            builder.EndStruct();
        }


        private void ReadArrayContent(IMessageReader reader, IObjectBuilder builder)
        {
            builder.BeginArray(reader.CollectionCount);

            var itemCount = reader.CollectionCount;

            for (; itemCount > 0; itemCount--)
            {
                builder.BeginItem();
                reader.Read();
                ReadValue(reader, builder);
                builder.EndItem();
            }
            builder.EndArray();
        }

    }
}