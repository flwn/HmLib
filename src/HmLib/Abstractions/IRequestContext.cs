namespace HmLib.Abstractions
{

    public interface IRequestContext
    {
        /// <summary>
        /// Binary, XmlRpc, Other?
        /// </summary>
        string Protocol { get; }

        IMessageReader Request { get; }
        IMessageBuilder Response { get; }
    }
}
