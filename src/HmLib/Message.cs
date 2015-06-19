using System;

namespace HmLib
{
    public abstract class Message
    {
        protected Message(MessageType messageType)
        {
            Type = messageType;
        }


        public virtual MessageType Type { get; protected set; }


        public static Message Create(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Unknown:
                default:
                    throw new ArgumentOutOfRangeException("messageType");

                case MessageType.Request:
                    return new Request();

                case MessageType.Response:
                    return new Response();

                case MessageType.Error:
                    return new ErrorResponse();
            }
        }
    }
}
