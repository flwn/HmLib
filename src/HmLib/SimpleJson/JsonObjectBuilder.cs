using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace HmLib.SimpleJson
{
    using Abstractions;

    public class JsonObjectBuilder : IObjectBuilder
    {

        private readonly TextWriter _writer;

        private const char Quote = '"';
        private const string True = "true";
        private const string False = "false";

        private Stack<BuilderState> _state = new Stack<BuilderState>(4);

        private List<bool> _firstObjectItemWritten = new List<bool>(4);

        public enum BuilderState : byte
        {
            Start,
            Struct,
            Array,
            Item,
        }

        public JsonObjectBuilder(TextWriter writer = null)
        {
            _writer = writer ?? new StringWriter(CultureInfo.InvariantCulture);

            Push(BuilderState.Start);
        }

        public void BeginStruct(int count)
        {
            Push(BuilderState.Struct);

            _writer.Write("{");
        }

        public void EndStruct()
        {
            Pop(BuilderState.Struct);

            _writer.Write("}");
        }
        public void BeginArray(int count)
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
            var currentState = Peek();
            if (currentState != BuilderState.Struct && currentState != BuilderState.Array)
            {
                throw new InvalidOperationException($"Invalid state, current state is {currentState} instead of the expected Struct Or Array.");
            }

            if (_firstObjectItemWritten[_firstObjectItemWritten.Count - 1])
            {
                WriteItemSeparation();
            }
            else
            {
                _firstObjectItemWritten[_firstObjectItemWritten.Count - 1] = true;
            }

            Push(BuilderState.Item);
        }

        public void EndItem()
        {
            Pop(BuilderState.Item);
        }

        public void WritePropertyName(string name)
        {
            var state = Peek();
            if (state != BuilderState.Item)
            {
                throw new InvalidOperationException($"Invalid State: cannot write property name while not in Item state (current state={state}).");
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
            _writer.Write(value ? True : False);
        }

        public override string ToString() => _writer.ToString();

        private void WriteItemSeparation()
        {
            _writer.Write(",");
        }


        private BuilderState Pop(BuilderState expectedState)
        {
            var currentState = Peek();
            if (currentState != expectedState)
            {
                throw new InvalidOperationException($"Invalid state: current state is {currentState} but {expectedState} was expected.");
            }

            var lastState = _state.Pop();

            if (lastState == BuilderState.Array || lastState == BuilderState.Struct)
            {
                _firstObjectItemWritten.RemoveAt(_firstObjectItemWritten.Count - 1);
            }

            return lastState;
        }


        private BuilderState Peek()
        {
            if (_state.Count == 0)
            {
                throw new InvalidOperationException("Invalid state: trying to peek while not in an Array, Struct or Item.");
            }
            return _state.Peek();
        }

        private void Push(BuilderState state)
        {
            if (state == BuilderState.Array || state == BuilderState.Struct)
            {
                _firstObjectItemWritten.Add(false);
            }
            _state.Push(state);
        }
    }
}
