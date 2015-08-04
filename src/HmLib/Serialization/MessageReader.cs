using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HmLib.Serialization
{
    using Abstractions;

    public class MessageReader : IMessageReader
    {

        private IEnumerator<ReadState> _messageEnumerator;
        private Message _input;

        public MessageReader(Response responseInput) : this()
        {
            if (responseInput == null) throw new ArgumentNullException(nameof(responseInput));

            _input = responseInput;

        }

        public MessageReader(Request requestInput) : this()
        {
            if (requestInput == null) throw new ArgumentNullException(nameof(requestInput));

            _input = requestInput;
        }

        protected MessageReader()
        {
            _messageEnumerator = EnvelopeReader().GetEnumerator();

        }

        public bool BooleanValue
        {
            get; private set;
        }

        public int ItemCount
        {
            get; private set;
        }

        public double DoubleValue
        {
            get; private set;
        }

        public int IntValue
        {
            get; private set;
        }

        public ReadState ReadState
        {
            get; private set;
        }

        public MessageType MessageType
        {
            get; private set;
        }

        public string PropertyName
        {
            get; private set;
        }

        public string StringValue
        {
            get; private set;
        }

        public ContentType? ValueType
        {
            get; private set;
        }

        public bool Read()
        {
            if (_messageEnumerator.MoveNext())
            {
                ReadState = _messageEnumerator.Current;
                return true;
            }
            return false;
        }

        private IEnumerable<ReadState> EnvelopeReader()
        {

            MessageType = _input.Type;
            yield return ReadState.Message;


            if (MessageType == MessageType.Request)
            {
                var request = (Request)_input;
                var headerCount = request.Headers.Count;
                if (headerCount > 0)
                {
                    ItemCount = headerCount;
                    yield return ReadState.Headers;

                    foreach (var kvp in request.Headers)
                    {
                        PropertyName = kvp.Key;
                        StringValue = kvp.Value;
                        yield return ReadState.Headers;
                    }
                }
            }

            yield return ReadState.Body;

            foreach (var contentType in BodyReader())
            {
                ValueType = contentType;
                yield return ReadState.Body;
            }

            yield return ReadState.EndOfFile;
        }

        private IEnumerable<ContentType> BodyReader()
        {
            if (MessageType == MessageType.Request)
            {
                var request = (Request)_input;

                PropertyName = null;
                StringValue = request.Method;
                yield return ContentType.String;

                ItemCount = request.Parameters.Count;
                yield return ContentType.Array;

                foreach (var type in request.Parameters.SelectMany(x => ReadValue(x)))
                {
                    yield return type;
                }

                yield break;
            }

            var response = (Response)_input;

            foreach (var type in ReadValue(response.Content))
            {
                yield return type;
            }

        }


        private IEnumerable<ContentType> ReadValue(object value)
        {
            if (value == null)
            {
                StringValue = string.Empty;
                yield return ContentType.String;
            }
            else if (value is string)
            {
                StringValue = (string)value;
                yield return ContentType.String;
            }
            else if (value is double)
            {
                DoubleValue = (double)value;
                yield return ContentType.Float;
            }
            else if (value is bool)
            {
                BooleanValue = (bool)value;
                yield return ContentType.Boolean;
            }
            else if (value is int)
            {
                IntValue = (int)value;
                yield return ContentType.Int;
            }
            else if (value is IEnumerable)
            {
                IEnumerable<object> inner;
                var kvpCollection = value as ICollection<KeyValuePair<string, object>>;
                var collection = value as ICollection<object>;
                if (kvpCollection != null)
                {
                    ItemCount = kvpCollection.Count;
                    yield return ContentType.Struct;

                    inner = ReadDictionary(kvpCollection);
                }
                else if (collection != null)
                {
                    ItemCount = collection.Count;
                    yield return ContentType.Array;
                    inner = collection;
                }
                else
                {
                    throw new NotSupportedException(string.Format("Collection '{0}' is not supported.", value.GetType()));
                }

                foreach (var item in inner.SelectMany(x => ReadValue(x)))
                {
                    yield return item;
                }
            }
            else
            {
                throw new NotSupportedException(string.Format("Type '{0}' is not supported.", value.GetType()));
            }
        }

        private IEnumerable<T> ReadDictionary<T>(ICollection<KeyValuePair<string, T>> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                PropertyName = kvp.Key;

                yield return kvp.Value;
            }
        }
    }
}
