using System;
using System.IO;
using System.Linq;
using System.Text;
using Shouldly;

namespace HmLib.Tests.Binary
{
    using HmLib.Binary;
    using HmLib.Serialization;

    public class BinaryProtocolTests
    {

        public void ShouldWriteErrorResponseCorrectly()
        {
            var errorMessage = "TEST123";
            var output = new MemoryStream();
            var protocol = new RequestResponseProtocol();
            protocol.WriteErrorResponse(new HmBinaryMessageWriter(output), errorMessage);

            output.Seek(0, SeekOrigin.Begin);
            var jsonOutput = new StringWriter();
            protocol.ReadResponse(new HmBinaryMessageReader(output), new JsonMessageBuilder(jsonOutput));

            var expected = "{\"faultCode\":-10,\"faultString\":\"" + errorMessage + "\"}";

            jsonOutput.ToString().ShouldBe(expected);
        }
        public void ShouldWriteStringResponseCorrectly()
        {
            var responseMessage = "TEST123";
            var output = new MemoryStream();
            var protocol = new RequestResponseProtocol();
            protocol.WriteResponse(new HmBinaryMessageWriter(output), responseMessage);

            output.Seek(0, SeekOrigin.Begin);
            var jsonOutput = new StringWriter();
            protocol.ReadResponse(new HmBinaryMessageReader(output), new JsonMessageBuilder(jsonOutput));

            var expected = "\"" + responseMessage + "\"";

            jsonOutput.ToString().ShouldBe(expected);
        }

        public void ShouldWriteRequestsCorrectly()
        {
            var testRequest = new Request()
            {
                Method = "system.listMethods",
                Parameters = { "Bla" }
            };
            testRequest.SetAuthorization("wiki", "pedia");


            var output = new MemoryStream();
            var protocol = new RequestResponseProtocol();
            protocol.WriteRequest(new HmBinaryMessageWriter(output), testRequest);

            var outputFormatted = BinaryUtils.FormatMemoryStream(output);

            var expected =
                "42696E400000002F000000010000000D417574686F72697A6174696F6E0000001642617369632064326C72615470775A57527059513D3D000000250000001273797374656D2E6C6973744D6574686F6473000000010000000300000003426C61";

            outputFormatted.ShouldBe(expected);
        }

        public void ShouldReadRequestsCorrectly()
        {
            var protocol = new RequestResponseProtocol();

            var requestByteString =
                "42696E400000002F000000010000000D417574686F72697A6174696F6E0000001642617369632064326C72615470775A57527059513D3D000000250000001273797374656D2E6C6973744D6574686F6473000000010000000300000003426C61";

            var requestStream = BinaryUtils.CreateByteStream(requestByteString);
            var requestReader = new HmBinaryMessageReader(requestStream);
            var messageBuilder = new MessageBuilder();

            protocol.ReadRequest(requestReader, messageBuilder);
            var request = messageBuilder.Result.ShouldBeOfType<Request>();
            request.Headers.Count.ShouldBe(1);
            var authHeader = string.Concat("Basic ", Convert.ToBase64String(Encoding.UTF8.GetBytes("wiki:pedia")));
            request.Headers.ShouldContainKeyAndValue("Authorization", authHeader);
            request.Method.ShouldBe("system.listMethods");
            request.Parameters.Count.ShouldBe(1);
            request.Parameters.First().ShouldBe("Bla");
        }

        public void BinaryWriterSupportsObjectBuilderInterface()
        {

            var protocol = new RequestResponseProtocol();

            var requestByteString =
                "42696E400000002F000000010000000D417574686F72697A6174696F6E0000001642617369632064326C72615470775A57527059513D3D000000250000001273797374656D2E6C6973744D6574686F6473000000010000000300000003426C61";

            var requestStream = BinaryUtils.CreateByteStream(requestByteString);

            var output = new MemoryStream();

            var requestReader = new HmBinaryMessageReader(requestStream);
            protocol.ReadRequest(requestReader, new HmBinaryMessageWriter(output));

            var outputFormatted = BinaryUtils.FormatMemoryStream(output);

            outputFormatted.ShouldBe(requestByteString);
        }


        public void ShouldReadVoidResponseCorrectly()
        {
            var responseByteString = "42696E0100000000";
            var responseStream = BinaryUtils.CreateByteStream(responseByteString);

            var protocol = new RequestResponseProtocol();
            var output = new StringWriter();
            var builder = new JsonMessageBuilder(output);
            protocol.ReadResponse(new HmBinaryMessageReader(responseStream), builder);

            var response = output.ToString();

            response.ShouldBe("");
        }

        public void ShouldReadResponseWithSimpleStringCorrectly()
        {
            var responseByteString = "42696E010000000B0000000300000003426C61";
            var responseStream = BinaryUtils.CreateByteStream(responseByteString);

            var protocol = new RequestResponseProtocol();
            var output = new StringWriter();
            var builder = new JsonMessageBuilder(output);
            protocol.ReadResponse(new HmBinaryMessageReader(responseStream), builder);

            var response = output.ToString();

            response.ShouldBe("\"Bla\"");
        }

        public void ShouldReadResponseWithVoidContent()
        {
            var voidString = "0000000300000000";
            var responseByteString = "42696E0100000008" + voidString;
            var responseStream = BinaryUtils.CreateByteStream(responseByteString);


            var protocol = new RequestResponseProtocol();
            var output = new StringWriter();
            var builder = new JsonMessageBuilder(output);
            protocol.ReadResponse(new HmBinaryMessageReader(responseStream), builder);

            var response = output.ToString();

            response.ShouldBe("\"\"");
        }

    }
}
