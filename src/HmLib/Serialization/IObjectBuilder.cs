﻿namespace HmLib.Serialization
{
    public interface IObjectBuilder
    {
        void BeginArray();
        void BeginItem();
        void BeginStruct();
        void EndArray();
        void EndItem();
        void EndStruct();
        void WriteBase64String(string base64String);
        void WriteBooleanValue(bool value);
        void WriteDoubleValue(double value);
        void WriteInt32Value(int value);
        void WritePropertyName(string name);
        void WriteStringValue(string value);
    }
}