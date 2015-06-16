
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HmLib.Tests.Binary
{
    using HmLib.Binary;
    using HmLib.Serialization;
    using Shouldly;

    public class HmBinaryProtocolTests
    {

        public void ShouldWriteRequestsCorrectly()
        {
            var testRequest = new Request()
            {
                Method = "system.listMethods",
                Parameters = { "Bla" }
            };
            testRequest.SetAuthorization("wiki", "pedia");

            var bufferStream = new MemoryStream();
            var protocol = new HmBinaryProtocol();
            protocol.WriteRequest(bufferStream, testRequest);
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

        public void ShouldReadRequestsCorrectly()
        {
            var protocol = new HmBinaryProtocol(() => new ObjectBuilder());

            var requestByteString =
                "42696E400000002F000000010000000D417574686F72697A6174696F6E0000001642617369632064326C72615470775A57527059513D3D000000250000001273797374656D2E6C6973744D6574686F6473000000010000000300000003426C61";

            var requestBytes = Enumerable.Range(0, requestByteString.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(requestByteString.Substring(x, 2), 16))
                     .ToArray();

            var requestStream = new MemoryStream(requestBytes);
            var request = protocol.ReadRequest(requestStream);

            request.ShouldNotBe(null);
            request.Method.ShouldBe("system.listMethods");
            request.Parameters.Count.ShouldBe(1);
            request.Parameters.First().ShouldBe("Bla");
        }

        public void BinaryWriterSupportsObjectBuilderInterface()
        {

            var output = new MemoryStream();
            var protocol = new HmBinaryProtocol(() => new HmBinaryWriter(output));

            var requestByteString =
                "42696E400000002F000000010000000D417574686F72697A6174696F6E0000001642617369632064326C72615470775A57527059513D3D000000250000001273797374656D2E6C6973744D6574686F6473000000010000000300000003426C61";


            var requestBytes = Enumerable.Range(0, requestByteString.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(requestByteString.Substring(x, 2), 16))
                     .ToArray();

            var requestStream = new MemoryStream(requestBytes);
            var request = protocol.ReadRequest(requestStream);

            var outputBuffer = output.ToArray();
            var outputFormatted = string.Join("", outputBuffer.Select(x => x.ToString("X2")));


            //value         hex
            //----------------------
            //arrayType     00000100
            //arrayCount    00000001
            //stringType    00000003
            //stringLength  00000003
            //Bla           42 6C 61

            outputFormatted.ShouldBe("00000100000000010000000300000003426C61");
        }
    }
}
