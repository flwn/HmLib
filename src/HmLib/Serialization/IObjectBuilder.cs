namespace HmLib.Serialization
{
    public interface IObjectBuilder
    {
        void BeginArray(int? count = null);
        void BeginItem();
        void BeginStruct(int? count = null);
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