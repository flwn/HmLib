using Shouldly;

namespace HmLib.Tests.Serialization
{
    using HmLib.Serialization;
    using System.Collections.Generic;
    using System.Linq;

    public class ObjectBuilderTests
    {
        private static void CreateEvent(MessageBuilder builder, string id, string device, string paramName, bool value)
        {
            builder.BeginStruct(2);
            builder.BeginItem();
            builder.WritePropertyName("method");
            builder.WriteStringValue("event");
            builder.EndItem();
            builder.BeginItem();
            builder.WritePropertyName("params");
            builder.BeginArray(4);
            builder.BeginItem();
            builder.WriteStringValue(id);
            builder.EndItem();
            builder.BeginItem();
            builder.WriteStringValue(device);
            builder.EndItem();
            builder.BeginItem();
            builder.WriteStringValue(paramName);
            builder.EndItem();
            builder.BeginItem();
            builder.WriteBooleanValue(value);
            builder.EndItem();
            builder.EndArray();
            builder.EndItem();
            builder.EndStruct();

        }

        public void MessageBuilderSupportsMulticall()
        {
            var builder = new MessageBuilder();

            builder.BeginMessage(MessageType.Request);
            builder.BeginContent();
            builder.BeginStruct(2);
            builder.BeginItem();
            builder.WritePropertyName("method");
            builder.WriteStringValue("system.multicall");
            builder.EndItem();
            builder.BeginItem();
            builder.WritePropertyName("params");

            builder.BeginArray(1);
            builder.BeginItem();

            //first param is array
            builder.BeginArray(2);
            builder.BeginItem();
            CreateEvent(builder, "HAUME-1230", "HEQ0359881:1", "ERROR_OVERLOAD", false);
            builder.EndItem();
            builder.BeginItem();
            CreateEvent(builder, "HAUME-1230", "HEQ0359881:2", "ERROR_OVERLOAD2", true);
            builder.EndItem();
            builder.EndArray();

            builder.EndItem();
            builder.EndArray();
            builder.EndItem();
            builder.EndStruct();
            builder.EndContent();

            builder.EndMessage();

            var request = builder.Result.ShouldBeAssignableTo<Request>();

            var content = request.Content.ShouldBeAssignableTo<IDictionary<string, object>>();

            content.ShouldContainKeyAndValue("method", "system.multicall");
            var @params = content["params"].ShouldBeAssignableTo<ICollection<object>>();
            @params.Count.ShouldBe(1);
            var requests = @params.First().ShouldBeAssignableTo<ICollection<object>>();
            requests.Count.ShouldBe(2);

            var firstEvent = requests.First().ShouldBeAssignableTo<IDictionary<string, object>>();
            firstEvent.ShouldContainKeyAndValue("method", "event");
            var firstEventParams = firstEvent["params"].ShouldBeAssignableTo<ICollection<object>>();
            firstEventParams.Count.ShouldBe(4);

            var secondEvent = requests.First().ShouldBeAssignableTo<IDictionary<string, object>>();
            secondEvent.ShouldContainKeyAndValue("method", "event");
            var secondEventParams = firstEvent["params"].ShouldBeAssignableTo<ICollection<object>>();
            secondEventParams.Count.ShouldBe(4);

        }

        public void MustBuildArrays()
        {
            var builder = new ObjectBuilder();

            builder.BeginArray(1);
            builder.BeginItem();
            builder.WriteStringValue("TEST");
            builder.EndItem();
            builder.EndArray();

            builder.CollectionResult.ShouldNotBe(null);

            builder.CollectionResult.ShouldContain("TEST");

            builder.StructResult.ShouldBe(null);
        }
    }
}
