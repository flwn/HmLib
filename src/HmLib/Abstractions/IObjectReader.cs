namespace HmLib.Abstractions
{
    public interface IObjectReader
    {
        bool Read();

        int ItemCount { get; }

        string PropertyName { get; }

        string StringValue { get; }

        int IntValue { get; }

        double DoubleValue { get; }

        bool BooleanValue { get; }

        ContentType? ValueType { get; }
    }
}