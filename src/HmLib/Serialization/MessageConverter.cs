using System;
using System.Collections.Generic;

using System.Reflection;

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
            [typeof(Binary.BinaryRequest)] = msg => new Binary.HmBinaryMessageWriter(new Binary.BinaryRequest()),
            [typeof(Binary.BinaryResponse)] = msg => new Binary.HmBinaryMessageWriter(new Binary.BinaryResponse()),
        };

        public TResponse Convert<TResponse>(IResponseMessage source) where TResponse : IResponseMessage
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return ConvertImpl<TResponse>(source);
        }
        public TResponse Convert<TResponse>(IResponseMessage source, IHasResult<TResponse> usingBuilder)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (usingBuilder == null) throw new ArgumentNullException(nameof(usingBuilder));

            return ConvertImpl(source, usingBuilder);
        }

        public TResult Convert<TResult>(IRequestMessage source, IHasResult<TResult> usingBuilder)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (usingBuilder == null) throw new ArgumentNullException(nameof(usingBuilder));

            return ConvertImpl(source, usingBuilder);
        }

        public TResult Convert<TResult>(IRequestMessage source) where TResult : IRequestMessage
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return ConvertImpl<TResult>(source);
        }



        private TResult ConvertImpl<TResult>(object source, IHasResult<TResult> writer = null)
        {
            Func<object, IMessageReader> readerFunc;


            if (source is IRequestMessage)
            {
                readerFunc = o => ((IRequestMessage)o).GetMessageReader();
            }
            else if (source is IResponseMessage)
            {
                readerFunc = o => ((IResponseMessage)o).GetMessageReader();
            }
            else
            {
                throw new InvalidOperationException();
            }

            if (writer == null)
            {
                Func<object, IMessageBuilder> builderFunc;
                if (false == _builders.TryGetValue(typeof(TResult), out builderFunc))
                {
                    return default(TResult);
                }
                writer = (IHasResult<TResult>)builderFunc(null);
            }

            var reader = readerFunc(source);

            Transformer.Transform(reader, writer);

            return writer.Result;
        }
    }
}
