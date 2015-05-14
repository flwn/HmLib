namespace HmLib
{
    public interface IHmStreamWriter
    {
        void Write(ContentType contentType);
        void Write(float value);
        void Write(double value);
        void Write(int value);
        void Write(bool value);
        void Write(string value);
        void Write(byte value);
        void Write(byte[] value);
    }
}