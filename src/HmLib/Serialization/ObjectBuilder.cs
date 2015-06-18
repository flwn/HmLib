using System;
using System.Collections.Generic;

namespace HmLib.Serialization
{

    public class ObjectBuilder : IObjectBuilder
    {
        public ICollection<object> CollectionResult { get; private set; }
        public IDictionary<string, object> StructResult { get; private set; }

        private Stack<object> _current = new Stack<object>();

        private Stack<Action<object>> _currentWriter = new Stack<Action<object>>();

        public void BeginArray(int? count = default(int?))
        {

            var list = new List<object>(count ?? 0);
            if (_current.Count == 0)
            {
                CollectionResult = list;
            }

            _current.Push(list);
            _currentWriter.Push(x => list.Add(x));
        }

        public void BeginItem()
        {
        }

        public void BeginStruct(int? count = null)
        {
            var dict = new Dictionary<string, object>(count ?? 0);
            if (_current.Count == 0)
            {
                StructResult = dict;
            }
            _current.Push(dict);
        }

        public void EndArray()
        {
            if (!(_current.Pop() is List<object>)) throw new InvalidOperationException("Expected list.");

            _currentWriter.Pop();
        }

        public void EndItem()
        {
        }

        public void EndStruct()
        {
            if (!(_current.Pop() is Dictionary<string, object>)) throw new InvalidOperationException("Expected Dictionary.");
            _currentWriter.Pop();
        }

        public void WriteBase64String(string base64String)
        {
            throw new NotImplementedException();
        }

        public void WriteBooleanValue(bool value)
        {
            WriteInternal(value);
        }

        public void WriteDoubleValue(double value)
        {
            WriteInternal(value);
        }

        public void WriteInt32Value(int value)
        {
            WriteInternal(value);
        }

        public void WritePropertyName(string name)
        {
            var dict = (IDictionary<string, object>)_current.Peek();

            _currentWriter.Push(x => dict[name] = x);
        }

        public void WriteStringValue(string value)
        {
            WriteInternal(value);
        }

        private void WriteInternal(object value)
        {
            _currentWriter.Peek()(value);
        }

    }
}
