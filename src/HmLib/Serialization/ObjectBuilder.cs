using System;
using System.Collections.Generic;

namespace HmLib.Serialization
{
    using Abstractions;

    public class ObjectBuilder : IObjectBuilder
    {
#if DEBUG
        public SimpleJson.JsonMessageBuilder Debug = new SimpleJson.JsonMessageBuilder();
#endif

        public ICollection<object> CollectionResult { get; private set; }

        public IDictionary<string, object> StructResult { get; private set; }

        public object SimpleResult { get; private set; }

        private Stack<object> _currentCollection = new Stack<object>();

        private Stack<Action<object>> _currentWriter = new Stack<Action<object>>();

        public ObjectBuilder()
        {
            _currentWriter.Push(x =>
            {
                if (SimpleResult != null)
                {
                    throw new InvalidOperationException();
                }
                SimpleResult = x;
            });
        }

        public void BeginArray(int count)
        {
#if DEBUG
            Debug.BeginArray(count);
#endif

            var list = new List<object>(count);
            if (_currentCollection.Count == 0)
            {
                CollectionResult = list;
            }
            else
            {
                WriteInternal(list);
            }

            _currentCollection.Push(list);
            _currentWriter.Push(x => list.Add(x));
        }

        public void BeginItem()
        {
#if DEBUG
            Debug.BeginItem();
#endif
        }

        public void BeginStruct(int count)
        {
#if DEBUG
            Debug.BeginStruct(count);
#endif
            var dict = new Dictionary<string, object>(count);
            if (_currentCollection.Count == 0)
            {
                StructResult = dict;
            }
            else
            {
                WriteInternal(dict);
            }
            _currentCollection.Push(dict);
        }

        public void EndArray()
        {
#if DEBUG
            Debug.EndArray();
#endif
            if (!(_currentCollection.Pop() is List<object>)) throw new InvalidOperationException("Expected list.");

            _currentWriter.Pop();
        }

        public void EndItem()
        {
#if DEBUG
            Debug.EndItem();
#endif
        }

        public void EndStruct()
        {
#if DEBUG
            Debug.EndStruct();
#endif
            if (!(_currentCollection.Pop() is Dictionary<string, object>)) throw new InvalidOperationException("Expected Dictionary.");
            _currentWriter.Pop();
        }

        public void WriteBase64String(string base64String)
        {
            throw new NotImplementedException("CANNOT WRITE BASE64 STRING");
        }

        public void WriteBooleanValue(bool value)
        {
#if DEBUG
            Debug.WriteBooleanValue(value);
#endif
            WriteInternal(value);
        }

        public void WriteDoubleValue(double value)
        {
#if DEBUG
            Debug.WriteDoubleValue(value);
#endif
            WriteInternal(value);
        }

        public void WriteInt32Value(int value)
        {
#if DEBUG
            Debug.WriteInt32Value(value);
#endif
            WriteInternal(value);
        }

        public void WritePropertyName(string name)
        {
#if DEBUG
            Debug.WritePropertyName(name);
#endif
            var dict = (IDictionary<string, object>)_currentCollection.Peek();

            _currentWriter.Push(x => dict[name] = x);
        }

        public void WriteStringValue(string value)
        {
#if DEBUG
            Debug.WriteStringValue(value);
#endif
            WriteInternal(value);
        }

        private void WriteInternal(object value)
        {
            if (_currentWriter.Count == 0)
            {
                System.Diagnostics.Debugger.Break();
            }
            _currentWriter.Peek()(value);
        }

    }
}
