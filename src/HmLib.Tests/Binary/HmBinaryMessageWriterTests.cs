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
            var expectedContent =
                string.Format("0000010100000001{0:X8}{1}00000003{2:X8}{3}", key.Length, keyBinary, value.Length, valueBinary);

            //expectedContent.Length / 2 because this is already in byte encoding (2 chars/byte);
            var expected = "42696E01" + (expectedContent.Length / 2).ToString("X8") + expectedContent;
            result.ShouldBe(expected);
        }
    }
}
