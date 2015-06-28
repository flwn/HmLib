using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HmLib.Binary
{
    internal static class Packet
    {
        public static readonly byte[] PacketHeader = { (byte)'B', (byte)'i', (byte)'n' };

        public const byte ErrorMessage = 0xff;
        public const byte RequestMessage = 0x00;
        public const byte RequestMessageWithHeaders = RequestMessage | MessageContainsHeaders;
        public const byte ResponseMessage = 0x01;
        public const byte ResponseMessageWithHeaders = ResponseMessage | MessageContainsHeaders;
        public const byte MessageContainsHeaders = 0x40;
    }
}
