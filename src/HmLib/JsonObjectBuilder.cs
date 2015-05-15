using System;
using System.Collections.Generic;
using System.IO;

namespace HmLib
{
    public class JsonObjectBuilder : IObjectBuilder
    {

        private readonly TextWriter _writer = new StringWriter();

        private const char Quote = '"';
        
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

        public void BeginStruct()
        {
            Push(BuilderState.Struct);

            _writer.Write("{");
        }

        public void EndStruct()
        {
            Pop(BuilderState.Struct);

            _writer.Write("}");
        }
        public void BeginArray()
        {
            Push(BuilderState.Array);

            _writer.Write("[");
        }

        public void EndArray()
        {
            Pop(BuilderState.Array);

            _writer.Write("]");
        }

        public void BeginItem()
        {
            var idx = _state.Count - 1;

            var currentState = Peek();
            if (currentState != BuilderState.Struct && currentState != BuilderState.Array)
            {
                throw new InvalidOperationException("Invalid state.");
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

        public void EndItem()
        {
            Pop(BuilderState.Item);
        }

        public void WritePropertyName(string name)
        {
            if (Peek() != BuilderState.Item)
            {
                throw new InvalidOperationException("Invalid state.");
            }

            _writer.Write(Quote);
            _writer.Write(name);
            _writer.Write(Quote);
            _writer.Write(":");
        }
        

        public void WriteStringValue(string value)
        {
            _writer.Write(Quote);
            _writer.Write(value);
            _writer.Write(Quote);
        }

        public void WriteBase64String(string base64String)
        {
            _writer.Write("\"base64,");
            _writer.Write(base64String);
            _writer.Write('"');
        }

        public void WriteInt32Value(int value)
        {
            _writer.Write(value);
        }

        public void WriteDoubleValue(double value)
        {
            _writer.Write(value);
        }

        public void WriteBooleanValue(bool value)
        {
            _writer.Write(value);
        }

        public override string ToString()
        {
            return _writer.ToString();
        }

        private void WriteItemSeparation()
        {
            _writer.Write(",");
        }


        private BuilderState Pop(BuilderState expectedState)
        {
            if (Peek() != expectedState)
            {
                throw new InvalidOperationException("Invalid state.");
            }

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
                throw new InvalidOperationException("Invalid state.");
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
    }
}
