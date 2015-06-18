using System;
using System.Collections.Generic;

namespace HmLib.Serialization
{

    public class ObjectBuilder : IObjectBuilder
    {
        public JsonMessageBuilder Debug = new JsonMessageBuilder();

        public ICollection<object> CollectionResult { get; private set; }
        public IDictionary<string, object> StructResult { get; private set; }

        public object SimpleResult { get; private set; }

        private Stack<object> _current = new Stack<object>();

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

        public void BeginArray(int? count = default(int?))
        {
            Debug.BeginArray();

            var list = new List<object>(count ?? 0);
            if (_current.Count == 0)
            {
                CollectionResult = list;
            }
            else
            {
                WriteInternal(list);
            }

            _current.Push(list);
            _currentWriter.Push(x => list.Add(x));
        }

        public void BeginItem()
        {
            Debug.BeginItem();
        }

        public void BeginStruct(int? count = null)
        {
            Debug.BeginStruct(count);
            var dict = new Dictionary<string, object>(count ?? 0);
            if (_current.Count == 0)
            {
                StructResult = dict;
            }
            else
            {
                WriteInternal(dict);
            }
            _current.Push(dict);
        }

        public void EndArray()
        {
            Debug.EndArray();
            if (!(_current.Pop() is List<object>)) throw new InvalidOperationException("Expected list.");

            _currentWriter.Pop();
        }

        public void EndItem()
        {
            Debug.EndItem();
        }

        public void EndStruct()
        {
            Debug.EndStruct();
            if (!(_current.Pop() is Dictionary<string, object>)) throw new InvalidOperationException("Expected Dictionary.");
            _currentWriter.Pop();
        }

        public void WriteBase64String(string base64String)
        {
            throw new NotImplementedException();
        }

        public void WriteBooleanValue(bool value)
        {
            Debug.WriteBooleanValue(value);
            WriteInternal(value);
        }

        public void WriteDoubleValue(double value)
        {
            Debug.WriteDoubleValue(value);
            WriteInternal(value);
        }

        public void WriteInt32Value(int value)
        {
            Debug.WriteInt32Value(value);
            WriteInternal(value);
        }

        public void WritePropertyName(string name)
        {
            Debug.WritePropertyName(name);
            var dict = (IDictionary<string, object>)_current.Peek();

            _currentWriter.Push(x => dict[name] = x);
        }

        public void WriteStringValue(string value)
        {
            Debug.WriteStringValue(value);
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
