using System;
using System.Net;

namespace HmLib
{
    public class ClientConnectionInfo
    {
        public IPEndPoint LocalEndPoint { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; }

        public DateTimeOffset Started { get; set; }
        public DateTimeOffset Finished { get; set; }
    }
}
