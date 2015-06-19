namespace HmLib.Serialization
{
    public interface IHmStreamReader
    {
        ContentType ReadContentType();

        bool ReadBoolean();

        double ReadDouble();

        int ReadInt32();

        string ReadString();
    }
}