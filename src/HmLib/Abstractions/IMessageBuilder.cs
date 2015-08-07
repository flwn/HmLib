﻿namespace HmLib.Abstractions
{
    public interface IMessageBuilder : IObjectBuilder
    {

        void BeginMessage(MessageType messageType);
        void EndMessage();

        void BeginHeaders(int headerCount);
        void WriteHeader(string key, string value);
        void EndHeaders();

        void BeginContent();
        void EndContent();
    }
}
