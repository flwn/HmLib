using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HmLib.Serialization
{
    /// <summary>
    /// Interface based on XmlSerializer.
    /// Serializes value a stream including type information.
    /// </summary>
    public class HmSerializer
    {
        internal readonly IObjectBuilder _debugOutput = new JsonObjectBuilder();

        private const uint OuterLevel = 0;
        private const uint InnerLevel = 1;

        private static readonly Type[] SupportedPrimitives = { typeof(bool), typeof(string), typeof(int), typeof(double) };

        private static readonly Type[] SupportedCollectionTypes =
        {
            typeof (IDictionary<string, string>), typeof (IDictionary<string, object>),
            typeof (ICollection), typeof (ICollection<object>),
            typeof (ICollection<string>), typeof (ICollection<bool>),
            typeof (ICollection<int>), typeof (ICollection<double>)
        };

        public enum WriteTypesFor : uint
        {
            Everything = 0,
            ChildElements = 1,
            Nothing,
        }


        public HmSerializer()
        {
            WriteTypeInfoLevel = WriteTypesFor.Everything;
        }

        public WriteTypesFor WriteTypeInfoLevel { get; set; }

        public bool CanSerialize(Type t)
        {
            if (t == null) throw new ArgumentNullException("t");

            return SupportedPrimitives.Contains(t) ||
                   SupportedCollectionTypes.Any(x => x.IsAssignableFrom(t));
        }

        public void Serialize(IHmStreamWriter writer, object o)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (o == null) return;

            if (!CanSerialize(o.GetType()))
            {
                throw new ArgumentException("Cannot serialize type.", "o");
            }

            SerializeInternal(writer, o, OuterLevel);
        }

        private void SerializeInternal(IHmStreamWriter writer, object o, uint level)
        {
            var writeType = level >= (int)WriteTypeInfoLevel;


            var structType = o as IDictionary<string, object>;
            if (structType != null)
            {
                SerializeStruct(writer, structType, writeType);
                return;
            }

            var stringDictionary = o as IDictionary<string, string>;
            if (stringDictionary != null)
            {
                SerializeStruct(writer, stringDictionary, writeType);
                return;
            }

            var collectionType = o as ICollection;
            if (collectionType != null)
            {
                SerializeCollection(writer, collectionType, writeType);
                return;
            }

            SerializeSimpleValue(writer, o, writeType);
        }

        private void SerializeCollection(IHmStreamWriter writer, ICollection listType, bool writeType)
        {
            _debugOutput.BeginArray(listType.Count);
            if (writeType)
            {
                writer.Write(ContentType.Array);
            }

            writer.Write(listType.Count);

            foreach (var item in listType)
            {
                _debugOutput.BeginItem();
                SerializeInternal(writer, item, InnerLevel);
                _debugOutput.EndItem();
            }
            _debugOutput.EndArray();
        }

        private void SerializeStruct<T>(IHmStreamWriter writer, IDictionary<string, T> structType, bool writeType)
        {
            _debugOutput.BeginStruct(structType.Count);

            if (writeType)
            {
                writer.Write(ContentType.Struct);
            }

            writer.Write(structType.Count);

            foreach (var kvp in structType)
            {
                _debugOutput.BeginItem();
                _debugOutput.WritePropertyName(kvp.Key);
                writer.Write(kvp.Key);

                SerializeInternal(writer, kvp.Value, InnerLevel);
                _debugOutput.EndItem();
            }
            _debugOutput.EndStruct();
        }

        private void SerializeSimpleValue(IHmStreamWriter writer, object value, bool writeType)
        {
            if (value == null)
            {
                throw new InvalidOperationException("Cannot write null value. Use empty string for void values.");
            }

            var stringValue = value as string;
            if (stringValue != null)
            {
                _debugOutput.WriteStringValue(stringValue);
                if (writeType)
                {
                    writer.Write(ContentType.String);
                }
                writer.Write(stringValue);
                return;
            }

            if (value is int)
            {
                if (writeType)
                {
                    writer.Write(ContentType.Int);
                }
                _debugOutput.WriteInt32Value((int)value);
                writer.Write((int)value);
                return;
            }

            if (value is bool)
            {
                if (writeType)
                {
                    writer.Write(ContentType.Boolean);
                }
                _debugOutput.WriteBooleanValue((bool)value);
                writer.Write((bool)value);
                return;
            }

            if (value is double)
            {
                if (writeType)
                {
                    writer.Write(ContentType.Float);
                }
                _debugOutput.WriteDoubleValue((double)value);
                writer.Write((double)value);
                return;
            }

            throw new InvalidOperationException("Type not supported");
        }

    }
}