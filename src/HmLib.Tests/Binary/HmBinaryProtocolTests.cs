
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HmLib.Tests.Binary
{
    using HmLib.Binary;
    using Shouldly;

    public class HmBinaryProtocolTests
    {

        public void ShouldWork()
        {
            var testRequest = new Request()
            {
                Method = "system.listMethods",
                Parameters = { "Bla" }
            };
            testRequest.SetAuthorization("wiki", "pedia");

            var bufferStream = new MemoryStream();
            new HmBinaryProtocol().WriteRequest(bufferStream, testRequest);
            var buffer = bufferStream.ToArray();
            var testoutput = new StringBuilder(buffer.Length * 2);

            foreach (var byte1 in buffer)
            {
                testoutput.AppendFormat("{0:X2}", byte1);
            }
            var expected =
                "42696E400000002F000000010000000D417574686F72697A6174696F6E0000001642617369632064326C72615470775A57527059513D3D000000250000001273797374656D2E6C6973744D6574686F6473000000010000000300000003426C61";

            testoutput.ToString().ShouldBe(expected);

        }
    }
}
