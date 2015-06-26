using System.Collections.Generic;
using System.Linq;
using Shouldly;

namespace HmLib.Tests.Serialization
{
    using HmLib.Abstractions;
    using HmLib.Serialization;


    public class MessageReaderTests
    {

        public void CanReadRequestsCorrectly()
        {

            var request = new Request
            {
                Method = "test",
                Parameters = { "param1", new List<object> { "nested param" } }
            };

            var reader = new MessageReader(request);

            //begin reading: read envelope
            reader.Read().ShouldBe(true);
            reader.ReadState.ShouldBe(ReadState.Message);
            reader.MessageType.ShouldBe(MessageType.Request);

            //move to headers or body
            reader.Read().ShouldBe(true);
            reader.ReadState.ShouldBe(ReadState.Body);

            //read body
            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.String);
            reader.StringValue.ShouldBe("test");
            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.Array);
            reader.ItemCount.ShouldBe(2);
            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.String);
            reader.StringValue.ShouldBe("param1");
            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.Array);
            reader.ItemCount.ShouldBe(1);
            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.String);
            reader.StringValue.ShouldBe("nested param");
            reader.Read().ShouldBe(true);
            reader.ReadState.ShouldBe(ReadState.EndOfFile);

            reader.Read().ShouldBe(false);
        }

        public void WorksWithRequestResponseProtocol()
        {

            var request = new Request
            {
                Method = "test",
                Parameters = { "param1", new List<object> { "nested param" } }
            };


            var reader = new MessageReader(request);


            var converter = new MessageConverter();
            var messageBuilder = new MessageBuilder();
            converter.Convert(reader, messageBuilder);

            var request2 = messageBuilder.Result.ShouldBeOfType<Request>();
            request2.Method.ShouldBe("test");
            request2.Parameters.Count.ShouldBe(2);
            request2.Parameters.First().ShouldBe("param1");

            var param2 = request2.Parameters.Skip(1).First().ShouldBeAssignableTo<ICollection<object>>();
            param2.First().ShouldBe("nested param");
        }
    }
}
