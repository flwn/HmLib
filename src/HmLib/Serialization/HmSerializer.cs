using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HmLib.Serialization
{
    using Abstractions;

    /// <summary>
    /// Interface based on XmlSerializer.
    /// Serializes value a stream including type information.
    /// </summary>
    public class HmSerializer
    {

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

        public HmSerializer()
        {
        }

        public bool CanSerialize(Type t)
        {
            if (t == null) throw new ArgumentNullException("t");

            return SupportedPrimitives.Contains(t) ||
                   SupportedCollectionTypes.Any(x => x.IsAssignableFrom(t));
        }

        public void Serialize(IObjectBuilder builder, object o)
        {
            if (builder == null) throw new ArgumentNullException("writer");
            if (o == null) return;

            if (!CanSerialize(o.GetType()))
            {
                throw new ArgumentException(string.Format("Type '{0}' is not supported.", o.GetType()), "o");
            }

            SerializeInternal(builder, o, OuterLevel);
        }

        private void SerializeInternal(IObjectBuilder builder, object o, uint level)
        {
            var structType = o as IDictionary<string, object>;
            if (structType != null)
            {
                SerializeStruct(builder, structType);
                return;
            }

            var stringDictionary = o as IDictionary<string, string>;
            if (stringDictionary != null)
            {
                SerializeStruct(builder, stringDictionary);
                return;
            }

            var collectionType = o as ICollection;
            if (collectionType != null)
            {
                SerializeCollection(builder, collectionType);
                return;
            }

            SerializeSimpleValue(builder, o);
        }

        private void SerializeCollection(IObjectBuilder builder, ICollection listType)
        {
            builder.BeginArray(listType.Count);

            foreach (var item in listType)
            {
                builder.BeginItem();
                SerializeInternal(builder, item, InnerLevel);
                builder.EndItem();
            }

            builder.EndArray();
        }

        private void SerializeStruct<T>(IObjectBuilder builder, IDictionary<string, T> structType)
        {
            builder.BeginStruct(structType.Count);

            foreach (var kvp in structType)
            {
                builder.BeginItem();
                builder.WritePropertyName(kvp.Key);

                SerializeInternal(builder, kvp.Value, InnerLevel);

                builder.EndItem();
            }
            builder.EndStruct();
        }

        private void SerializeSimpleValue(IObjectBuilder builder, object value)
        {
            if (value == null)
            {
                throw new InvalidOperationException("Cannot write null value. Use empty string for void values.");
            }

            var stringValue = value as string;
            if (stringValue != null)
            {
                builder.WriteStringValue(stringValue);
                return;
            }

            if (value is int)
            {
                builder.WriteInt32Value((int)value);
                return;
            }

            if (value is bool)
            {
                builder.WriteBooleanValue((bool)value);
                return;
            }

            if (value is double)
            {
                builder.WriteDoubleValue((double)value);
                return;
            }

            throw new InvalidOperationException("Type not supported");
        }

    }
}