using System;
using System.Collections.Generic;

namespace HmLib.Serialization
{
    using Abstractions;

    public class MessageConverter : IMessageConverter
    {
        private IDictionary<Type, Func<object, IMessageReader>> _readers = new Dictionary<Type, Func<object, IMessageReader>>
        {
            [typeof(Request)] = msg => new MessageReader((Request)msg),
            [typeof(Response)] = msg => new MessageReader((Response)msg),
        };

        private IDictionary<Type, Func<object, IMessageBuilder>> _builders = new Dictionary<Type, Func<object, IMessageBuilder>>
        {
            [typeof(Request)] = msg => new MessageBuilder((Request)msg),
            [typeof(Response)] = msg => new MessageBuilder((Response)msg),
        };

        public TResponse Convert<TResponse>(IResponseMessage source) where TResponse : IResponseMessage
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return ConvertImpl<TResponse>(source);
        }
        public void Convert(IResponseMessage source, IMessageBuilder usingBuilder)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (usingBuilder == null) throw new ArgumentNullException(nameof(usingBuilder));

            ConvertImpl<object>(source, usingBuilder);
        }

        public void Convert(IRequestMessage source, IMessageBuilder usingBuilder)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (usingBuilder == null) throw new ArgumentNullException(nameof(usingBuilder));

            ConvertImpl<object>(source, usingBuilder);
        }

        public TResult Convert<TResult>(IRequestMessage source) where TResult : IRequestMessage
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return ConvertImpl<TResult>(source);
        }

        public void Convert<TResult>(IRequestMessage source, TResult targetMessage)
            where TResult : IRequestMessage
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (targetMessage == null) throw new ArgumentNullException(nameof(targetMessage));

            if (source is IFastCopyTo<TResult>)
            {
                ((IFastCopyTo<TResult>)source).CopyTo(targetMessage);
            }

            Func<object, IMessageBuilder> builderFunc;
            if (false == _builders.TryGetValue(typeof(TResult), out builderFunc))
            {
                return;
            }
            var writer = builderFunc(targetMessage);

            if (writer == null)
            {
                return;
            }

            ConvertImpl<TResult>(source, writer);
        }

        private TResult ConvertImpl<TResult>(object source, IMessageBuilder writer = null)
        {
            Func<object, IMessageReader> readerFunc;
            if (false == _readers.TryGetValue(source.GetType(), out readerFunc))
            {
                return default(TResult);
            }

            if (writer == null)
            {
                Func<object, IMessageBuilder> builderFunc;
                if (false == _builders.TryGetValue(typeof(TResult), out builderFunc))
                {
                    return default(TResult);
                }
                writer = builderFunc(null);
            }

            var reader = readerFunc(source);

            Convert(reader, writer);

            if (writer is IHasResult<TResult>)
            {
                return ((IHasResult<TResult>)writer).Result;
            }

            return default(TResult);
        }

        public void Convert(IMessageReader input, IMessageBuilder output)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (output == null) throw new ArgumentNullException(nameof(output));

            ConvertInternal(input, output);
        }

        private void ConvertInternal(IMessageReader input, IMessageBuilder output)
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

        private void ConvertArrayContent(IMessageReader reader, IObjectBuilder builder)
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

        private void ConvertValue(IMessageReader reader, IObjectBuilder builder)
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

        private void ConvertStructContent(IMessageReader reader, IObjectBuilder builder)
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


        private void ConvertHeaders(IMessageReader input, IMessageBuilder output)
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
