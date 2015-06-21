using System.IO;
using System.Text;
using Shouldly;

namespace HmLib.Tests.Binary
{
    using _Infrastructure;
    using HmLib.Binary;
    using HmLib.Serialization;

    public class HmBinaryMessageReaderTests
    {

        public void StringResponsesAreWrittenCorrectly()
        {
            var input = new MemoryStream();
            var writer = new HmBinaryMessageWriter(input);

            writer.BeginMessage(MessageType.Request);
            writer.BeginContent();
            writer.SetMethod("newDevices");
            writer.BeginArray(1);
            writer.BeginItem();

            //parameter 1
            writer.BeginArray(2);

            //writer.BeginItem();
            //writer.WriteStringValue("TEST-0306");
            //writer.EndItem();

            writer.BeginItem();
            writer.BeginStruct(2);
            //writer.BeginItem();
            //writer.WritePropertyName("ADDRESS");
            //writer.WriteStringValue("BidCoS-RF");
            //writer.EndItem();
            writer.BeginItem();
            writer.WritePropertyName("CHILDREN");
            var items = 3;
            writer.BeginArray(items);
            for (; items > 0; items--)
            {
                writer.BeginItem();
                writer.WriteStringValue("value " + items);
                writer.EndItem();
            }
            writer.EndArray();
            writer.EndItem();
            writer.BeginItem();
            writer.WritePropertyName("FIRMWARE");
            writer.WriteStringValue("1.505");
            writer.EndItem();
            writer.EndStruct();
            writer.EndItem();

            writer.BeginItem();
            writer.BeginStruct(3);
            writer.BeginItem();
            writer.WritePropertyName("ADDRESS");
            writer.WriteStringValue("BidCoS-RF");
            writer.EndItem();
            writer.BeginItem();
            writer.WritePropertyName("CHILDREN");
            var items2 = 5;
            writer.BeginArray(items2);
            for (; items2 > 0; items2--)
            {
                writer.BeginItem();
                writer.WriteStringValue("value " + items2);
                writer.EndItem();
            }
            writer.EndArray();
            writer.EndItem();
            writer.BeginItem();
            writer.WritePropertyName("FIRMWARE");
            writer.WriteStringValue("1.505");
            writer.EndItem();
            writer.EndStruct();
            writer.EndItem();

            //end parameter 1
            writer.EndArray();
            writer.EndItem();

            //end params
            writer.EndArray();
            writer.EndContent();
            writer.EndMessage();
            var result = BinaryUtils.FormatMemoryStream(input);

            input.Seek(0, SeekOrigin.Begin);
            var protocol = new RequestResponseProtocol();
            var outputReader = new JsonMessageBuilder();
            protocol.ReadRequest(new HmBinaryMessageReader(input), outputReader);

            //input.Seek(0, SeekOrigin.Begin);
            //var reader = new HmBinaryStreamReader(input);
            //var tokenized = BinaryUtils.Tokenize(reader);

            result.ShouldBe("42696E00000001180000000A6E6577446576696365730000000100000100000000020000010100000002000000084348494C4452454E0000010000000003000000030000000776616C75652033000000030000000776616C75652032000000030000000776616C75652031000000084649524D574152450000000300000005312E353035000001010000000300000007414444524553530000000300000009426964436F532D5246000000084348494C4452454E0000010000000005000000030000000776616C75652035000000030000000776616C75652034000000030000000776616C75652033000000030000000776616C75652032000000030000000776616C75652031000000084649524D574152450000000300000005312E353035");
        }


    }
}
