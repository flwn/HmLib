using System;
using System.Collections.Generic;
using System.IO;

namespace HmLib
{
    public class JsonObjectBuilder
    {

        private TextWriter _writer = new StringWriter();

        private const char Quote = '"';

        //cannot use Stack<T> here because ObjectBuilderState is a value type
        private Stack<BuilderState> _state = new Stack<BuilderState>(4);

        private List<bool> _objectState = new List<bool>(4);

        public enum BuilderState : byte
        {
            Start,
            Struct,
            Array,
            Item,
        }

        public JsonObjectBuilder(TextWriter writer = null)
        {
            _writer = writer ?? new StringWriter();

            Push(BuilderState.Start);
        }

        internal void BeginStruct()
        {
            Push(BuilderState.Struct);
            _writer.Write("{");
        }

        internal void EndStruct()
        {
            var state = Pop();
            if (state != BuilderState.Struct)
            {
                throw new InvalidOperationException("");
            }
            _writer.Write("}");
        }
        internal void BeginArray()
        {
            Push(BuilderState.Array);

            _writer.Write("[");
        }

        internal void EndArray()
        {
            var state = Pop();
            if (state != BuilderState.Array)
            {
                throw new InvalidOperationException("");
            }
            _writer.Write("]");
        }

        internal void WriteItemSeparation()
        {
            _writer.Write(",");
        }

        internal void BeginItem()
        {
            var idx = _state.Count - 1;

            var currentState = Peek();
            if (currentState != BuilderState.Struct && currentState != BuilderState.Array)
            {
                throw new InvalidOperationException("");
            }

            if (_objectState[_objectState.Count - 1])
            {
                WriteItemSeparation();
            }
            else
            {
                _objectState[_objectState.Count - 1] = true;
            }

            Push(BuilderState.Item);
        }

        internal void EndItem()
        {
            var currentState = Pop();
            if (currentState != BuilderState.Item)
            {
                throw new InvalidOperationException("");
            }
        }

        internal void WritePropertyName(string name)
        {
            if (Peek() != BuilderState.Item)
            {
                throw new InvalidOperationException("");
            }

            _writer.Write(Quote);
            _writer.Write(name);
            _writer.Write(Quote);
            _writer.Write(":");
        }


        private BuilderState Pop()
        {
            var lastState = _state.Pop();

            if (lastState == BuilderState.Array || lastState == BuilderState.Struct)
            {
                _objectState.RemoveAt(_objectState.Count - 1);
            }

            return lastState;
        }


        private BuilderState Peek()
        {
            if (_state.Count == 0)
            {
                throw new InvalidOperationException("Invalid state");
            }
            return _state.Peek();
        }

        private void Push(BuilderState state)
        {
            if (state == BuilderState.Array || state == BuilderState.Struct)
            {
                _objectState.Add(false);
            }
            _state.Push(state);
        }

        internal void WriteStringValue(string stringValue)
        {
            _writer.Write(Quote);
            _writer.Write(stringValue);
            _writer.Write(Quote);
        }

        internal void WriteBase64String(string base64String)
        {
            _writer.Write("\"base64,");
            _writer.Write(base64String);
            _writer.Write('"');
        }

        internal void WriteInt32Value(int value)
        {
            _writer.Write(value);
        }

        internal void WriteDoubleValue(double value)
        {
            _writer.Write(value);
        }

        internal void WriteBooleanValue(bool value)
        {
            _writer.Write(value);
        }

        public override string ToString()
        {
            return _writer.ToString();
        }
    }
}
