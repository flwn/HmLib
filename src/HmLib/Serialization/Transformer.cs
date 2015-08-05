using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HmLib.Serialization
{
    using Abstractions;

    public static class Transformer
    {

        public static void Transform(IMessageReader input, IMessageBuilder output)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (output == null) throw new ArgumentNullException(nameof(output));

            ConvertInternal(input, output);
        }

        private static void ConvertInternal(IMessageReader input, IMessageBuilder output)
        {
            if (!input.Read())
            {
                throw new ProtocolException("Packet Header not recognized.");
            }

            output.BeginMessage(input.MessageType);

            input.Read();
            if (input.ReadState == ReadState.Headers)
            {
                ConvertHeaders(input, output);
                input.Read();
            }

            output.BeginContent();
            if (input.MessageType == MessageType.Request)
            {
                input.Read();
                output.SetMethod(input.StringValue);

                input.Read();
                ConvertArrayContent(input, output);
            }
            else
            {
                while (input.Read() && input.ReadState == ReadState.Body)
                {
                    ConvertValue(input, output);
                }
            }
            output.EndContent();
            output.EndMessage();

            input.Read();
            if (input.ReadState != ReadState.EndOfFile)
            {
                throw new ProtocolException("Expected EndOfFile");
            }
        }

        private static void ConvertArrayContent(IMessageReader reader, IObjectBuilder builder)
        {

            var itemCount = reader.ItemCount;
            builder.BeginArray(itemCount);

            for (; itemCount > 0; itemCount--)
            {
                builder.BeginItem();
                reader.Read();

                ConvertValue(reader, builder);
                builder.EndItem();
            }

            builder.EndArray();
        }

        private static void ConvertValue(IMessageReader reader, IObjectBuilder builder)
        {
            switch (reader.ValueType)
            {
                case ContentType.Array:
                    ConvertArrayContent(reader, builder);
                    break;
                case ContentType.Struct:
                    ConvertStructContent(reader, builder);
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

        private static void ConvertStructContent(IMessageReader reader, IObjectBuilder builder)
        {
            var elementCount = reader.ItemCount;
            builder.BeginStruct(reader.ItemCount);

            for (; elementCount > 0; elementCount--)
            {
                builder.BeginItem();
                reader.Read();

                builder.WritePropertyName(reader.PropertyName);

                ConvertValue(reader, builder);
                builder.EndItem();
            }

            builder.EndStruct();
        }


        private static void ConvertHeaders(IMessageReader input, IMessageBuilder output)
        {
            var headerCount = input.ItemCount;
            output.BeginHeaders(headerCount);

            for (; headerCount > 0; headerCount--)
            {
                input.Read();
                output.WriteHeader(input.PropertyName, input.StringValue);
            }
            output.EndHeaders();
        }
    }
}
