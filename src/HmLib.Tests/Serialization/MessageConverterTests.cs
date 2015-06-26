﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using Shouldly;

namespace HmLib.Tests.Serialization
{
    using _Infrastructure;
    using HmLib.Binary;
    using HmLib.Serialization;
    using HmLib.SimpleJson;

    public class BinaryProtocolTests
    {

        public void ShouldWriteErrorResponseCorrectly()
        {
            var errorMessage = "TEST123";
            var output = new MemoryStream();
            var converter = new MessageConverter();
            var errorResponse = new ErrorResponse { Code = -10, Message = errorMessage };
            var errorReader = new MessageReader(errorResponse);
            converter.Convert(errorReader, new HmBinaryMessageWriter(output));

            output.Seek(0, SeekOrigin.Begin);
            var jsonOutput = new StringWriter();

            converter.Convert(new HmBinaryMessageReader(output), new JsonMessageBuilder(jsonOutput));

            var expected = "{\"faultCode\":-10,\"faultString\":\"" + errorMessage + "\"}";

            jsonOutput.ToString().ShouldBe(expected);
        }
        public void ShouldWriteStringResponseCorrectly()
        {
            var responseMessage = "TEST123";
            var output = new MemoryStream();
            var converter = new MessageConverter();
            var response = new Response { Content = responseMessage };
            converter.Convert(new MessageReader(response), new HmBinaryMessageWriter(output));

            output.Seek(0, SeekOrigin.Begin);
            var jsonOutput = new StringWriter();
            converter.Convert(new HmBinaryMessageReader(output), new JsonMessageBuilder(jsonOutput));

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
            var converter = new MessageConverter();
            converter.Convert(new MessageReader(testRequest), new HmBinaryMessageWriter(output));

            var outputFormatted = BinaryUtils.FormatMemoryStream(output);

            var expected =
                "42696E400000002F000000010000000D417574686F72697A6174696F6E0000001642617369632064326C72615470775A57527059513D3D000000250000001273797374656D2E6C6973744D6574686F6473000000010000000300000003426C61";

            outputFormatted.ShouldBe(expected);
        }

        public void ShouldReadRequestsCorrectly()
        {
            var converter = new MessageConverter();

            var requestByteString =
                "42696E400000002F000000010000000D417574686F72697A6174696F6E0000001642617369632064326C72615470775A57527059513D3D000000250000001273797374656D2E6C6973744D6574686F6473000000010000000300000003426C61";

            var requestStream = BinaryUtils.CreateByteStream(requestByteString);
            var requestReader = new HmBinaryMessageReader(requestStream);
            var messageBuilder = new MessageBuilder();

            converter.Convert(requestReader, messageBuilder);
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

            var converter = new MessageConverter();

            var requestByteString =
                "42696E400000002F000000010000000D417574686F72697A6174696F6E0000001642617369632064326C72615470775A57527059513D3D000000250000001273797374656D2E6C6973744D6574686F6473000000010000000300000003426C61";

            var requestStream = BinaryUtils.CreateByteStream(requestByteString);

            var output = new MemoryStream();

            var requestReader = new HmBinaryMessageReader(requestStream);
            converter.Convert(requestReader, new HmBinaryMessageWriter(output));

            var outputFormatted = BinaryUtils.FormatMemoryStream(output);

            outputFormatted.ShouldBe(requestByteString);
        }


        public void ShouldReadVoidResponseCorrectly()
        {
            var responseByteString = "42696E0100000000";
            var responseStream = BinaryUtils.CreateByteStream(responseByteString);

            var converter = new MessageConverter();
            var output = new StringWriter();
            var builder = new JsonMessageBuilder(output);
            converter.Convert(new HmBinaryMessageReader(responseStream), builder);

            var response = output.ToString();

            response.ShouldBe("");
        }

        public void ShouldReadResponseWithSimpleStringCorrectly()
        {
            var responseByteString = "42696E010000000B0000000300000003426C61";
            var responseStream = BinaryUtils.CreateByteStream(responseByteString);

            var converter = new MessageConverter();
            var output = new StringWriter();
            var builder = new JsonMessageBuilder(output);
            converter.Convert(new HmBinaryMessageReader(responseStream), builder);

            var response = output.ToString();

            response.ShouldBe("\"Bla\"");
        }

        public void ShouldReadResponseWithVoidContent()
        {
            var voidString = "0000000300000000";
            var responseByteString = "42696E0100000008" + voidString;
            var responseStream = BinaryUtils.CreateByteStream(responseByteString);


            var converter = new MessageConverter();
            var output = new StringWriter();
            var builder = new JsonMessageBuilder(output);
            converter.Convert(new HmBinaryMessageReader(responseStream), builder);

            var response = output.ToString();

            response.ShouldBe("\"\"");
        }

    }
}
