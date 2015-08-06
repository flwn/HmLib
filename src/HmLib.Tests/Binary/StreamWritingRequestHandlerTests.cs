using System.IO;
using System.Linq;
using Shouldly;

namespace HmLib.Tests.Binary
{
    using _Infrastructure;
    using HmLib.Binary;

    public class StreamWritingRequestHandlerTests
    {

        public void HandleRequestsCorrectlyWritesResponse()
        {
            var expectedRequestBytes = BinaryUtils.CreateByteArray("42696E000000001300000100000000010000000300000003426C61");
            var expectedResponseBytes = BinaryUtils.CreateByteArray("42696E0100000000");


            var memoryStream = new MemoryStream(expectedResponseBytes.Length + expectedRequestBytes.Length);

            //fill the memoryStream with the response, after the part where the request will be written.
            memoryStream.Position = expectedRequestBytes.Length;
            memoryStream.Write(expectedResponseBytes, 0, expectedResponseBytes.Length);
            memoryStream.Position = 0L;

            var requestHandler = new StreamWritingRequestHandler(memoryStream);


            var request = new BinaryRequest(new MemoryStream(expectedRequestBytes));

            var response = requestHandler.HandleRequest(request).Result;

            memoryStream.Position.ShouldBe(expectedRequestBytes.Length + expectedResponseBytes.Length);

            var binaryResponse = response.ShouldBeOfType<BinaryResponse>();

            var memoryBuffer = memoryStream.ToArray();


            memoryBuffer.Take(expectedRequestBytes.Length).ShouldBe(expectedRequestBytes);

            ((MemoryStream)binaryResponse.MessageStream).ToArray().ShouldBe(expectedResponseBytes);
        }
    }
}
