using System.IO;
using System.Text;
using Shouldly;

namespace HmLib.Tests.Binary
{
    using _Infrastructure;
    using HmLib.Binary;

    public class HmBinaryMessageWriterTests
    {

        public void StringResponsesAreWrittenCorrectly()
        {
            var output = new MemoryStream();
            var writer = new HmBinaryMessageWriter(output, true);

            writer.BeginMessage(MessageType.Response);
            writer.BeginContent();
            writer.WriteStringValue("Bla");
            writer.EndContent();
            writer.EndMessage();

            var result = BinaryUtils.FormatMemoryStream(output);

            result.ShouldBe("42696E010000000B0000000300000003426C61");
        }
        public void CollectionResponsesAreWrittenCorrectly()
        {
            var output = new MemoryStream();
            var writer = new HmBinaryMessageWriter(output, true);

            writer.BeginMessage(MessageType.Response);
            writer.BeginContent();
            writer.BeginArray(1);
            writer.BeginItem();
            writer.WriteStringValue("Bla");
            writer.EndItem();
            writer.EndArray();
            writer.EndContent();
            writer.EndMessage();

            var result = BinaryUtils.FormatMemoryStream(output);

            result.ShouldBe("42696E010000001300000100000000010000000300000003426C61");
        }

        [Input("key", "value")]
        public void StructResponsesAreWrittenCorrectly(string key, string value)
        {
            var output = new MemoryStream();
            var writer = new HmBinaryMessageWriter(output, true);


            writer.BeginMessage(MessageType.Response);
            writer.BeginContent();
            writer.BeginStruct(1);
            writer.BeginItem();
            writer.WritePropertyName(key);
            writer.WriteStringValue(value);
            writer.EndItem();
            writer.EndStruct();
            writer.EndContent();
            writer.EndMessage();

            var result = BinaryUtils.FormatMemoryStream(output);

            var keyBinary = BinaryUtils.FormatByteArray(Encoding.ASCII.GetBytes(key));
            var valueBinary = BinaryUtils.FormatByteArray(Encoding.ASCII.GetBytes(value));
            var expectedContent = $"0000010100000001{key.Length:X8}{keyBinary}00000003{value.Length:X8}{valueBinary}";

            //expectedContent.Length / 2 because this is already in byte encoding (2 chars/byte);
            var expected = "42696E01" + (expectedContent.Length / 2).ToString("X8") + expectedContent;
            result.ShouldBe(expected);
        }



        public void ShouldWriteComplexRequestCorrectly()
        {
            var input = new MemoryStream();
            var writer = new HmBinaryMessageWriter(input);

            writer.BeginMessage(MessageType.Request);
            writer.BeginContent();

            writer.BeginStruct(2);
            writer.BeginItem();
            writer.WritePropertyName("method");
            writer.WriteStringValue("newDevices");
            writer.EndItem();
            writer.BeginItem();
            writer.WritePropertyName("parameters");

//            writer.SetMethod("newDevices");
            writer.BeginArray(1);
            writer.BeginItem();

            //parameter 1
            writer.BeginArray(2);

            writer.BeginItem();
            writer.BeginStruct(2);
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

            writer.EndItem();
            writer.EndStruct();

            writer.EndContent();
            writer.EndMessage();
            var result = BinaryUtils.FormatMemoryStream(input);

            result.ShouldBe("42696E00000001180000000A6E6577446576696365730000000100000100000000020000010100000002000000084348494C4452454E0000010000000003000000030000000776616C75652033000000030000000776616C75652032000000030000000776616C75652031000000084649524D574152450000000300000005312E353035000001010000000300000007414444524553530000000300000009426964436F532D5246000000084348494C4452454E0000010000000005000000030000000776616C75652035000000030000000776616C75652034000000030000000776616C75652033000000030000000776616C75652032000000030000000776616C75652031000000084649524D574152450000000300000005312E353035");
        }

    }
}
