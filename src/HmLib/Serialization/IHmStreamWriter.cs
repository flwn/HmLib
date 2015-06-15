namespace HmLib.Serialization
{
    public interface IHmStreamWriter
    {
        void Write(ContentType contentType);
        void Write(double value);
        void Write(int value);
        void Write(bool value);
        void Write(string value);
    }
}