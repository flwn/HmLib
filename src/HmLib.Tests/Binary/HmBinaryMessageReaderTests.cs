using System;
using System.Text;
using Shouldly;

namespace HmLib.Tests.Binary
{
    using _Infrastructure;
    using HmLib.Abstractions;
    using HmLib.Binary;

    public class HmBinaryMessageReaderTests
    {
        public void ReadEmitsCorrectData()
        {
            const string encodedInput = "42696E00000001180000000A6E6577446576696365730000000100000100000000020000010100000002000000084348494C4452454E0000010000000003000000030000000776616C75652033000000030000000776616C75652032000000030000000776616C75652031000000084649524D574152450000000300000005312E353035000001010000000300000007414444524553530000000300000009426964436F532D5246000000084348494C4452454E0000010000000005000000030000000776616C75652035000000030000000776616C75652034000000030000000776616C75652033000000030000000776616C75652032000000030000000776616C75652031000000084649524D574152450000000300000005312E353035";

            var input = BinaryUtils.CreateByteStream(encodedInput);

            var reader = new HmBinaryMessageReader(input);

            reader.ReadState.ShouldBe(ReadState.Initial);

            reader.Read().ShouldBe(true);
            reader.ReadState.ShouldBe(ReadState.Message);
            reader.MessageType.ShouldBe(MessageType.Request);

            reader.Read().ShouldBe(true);
            reader.ReadState.ShouldBe(ReadState.Body);


            //read body
            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.Struct);
            reader.ItemCount.ShouldBe(2);

            reader.Read().ShouldBe(true);
            reader.PropertyName.ShouldBe("method");
            reader.ValueType.ShouldBe(ContentType.String);
            reader.StringValue.ShouldBe("newDevices");

            reader.Read().ShouldBe(true);
            reader.PropertyName.ShouldBe("parameters");
            reader.ValueType.ShouldBe(ContentType.Array);
            reader.ItemCount.ShouldBe(1);

            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.Array);
            reader.ItemCount.ShouldBe(2);
            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.Struct);
            reader.ItemCount.ShouldBe(2);

            reader.Read().ShouldBe(true);
            reader.PropertyName.ShouldBe("CHILDREN");
            reader.ValueType.ShouldBe(ContentType.Array);
            reader.ItemCount.ShouldBe(3);
            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.String);
            reader.StringValue.ShouldBe("value 3");
            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.String);
            reader.StringValue.ShouldBe("value 2");
            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.String);
            reader.StringValue.ShouldBe("value 1");


            reader.Read().ShouldBe(true);
            reader.PropertyName.ShouldBe("FIRMWARE");
            reader.ValueType.ShouldBe(ContentType.String);
            reader.StringValue.ShouldBe("1.505");


            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.Struct);
            reader.ItemCount.ShouldBe(3);
            reader.Read().ShouldBe(true);
            reader.PropertyName.ShouldBe("ADDRESS");
            reader.ValueType.ShouldBe(ContentType.String);
            reader.StringValue.ShouldBe("BidCoS-RF");

            reader.Read().ShouldBe(true);
            reader.PropertyName.ShouldBe("CHILDREN");
            reader.ValueType.ShouldBe(ContentType.Array);
            reader.ItemCount.ShouldBe(5);
            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.String);
            reader.StringValue.ShouldBe("value 5");
            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.String);
            reader.StringValue.ShouldBe("value 4");
            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.String);
            reader.StringValue.ShouldBe("value 3");
            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.String);
            reader.StringValue.ShouldBe("value 2");
            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.String);
            reader.StringValue.ShouldBe("value 1");

            reader.Read().ShouldBe(true);
            reader.PropertyName.ShouldBe("FIRMWARE");
            reader.ValueType.ShouldBe(ContentType.String);
            reader.StringValue.ShouldBe("1.505");

            reader.Read().ShouldBe(true);
            reader.ReadState.ShouldBe(ReadState.EndOfFile);

            reader.Read().ShouldBe(false);
        }

        public void ShouldReadSimpleRequestWithHeaders()
        {

            var requestByteString =
                "42696E400000002F000000010000000D417574686F72697A6174696F6E0000001642617369632064326C72615470775A57527059513D3D000000250000001273797374656D2E6C6973744D6574686F6473000000010000000300000003426C61";

            var requestStream = BinaryUtils.CreateByteStream(requestByteString);
            var reader = new HmBinaryMessageReader(requestStream);

            reader.ReadState.ShouldBe(ReadState.Initial);

            reader.Read().ShouldBe(true);
            reader.ReadState.ShouldBe(ReadState.Message);
            reader.MessageType.ShouldBe(MessageType.Request);

            //read headers
            reader.Read().ShouldBe(true);
            reader.ReadState.ShouldBe(ReadState.Headers);
            reader.ItemCount.ShouldBe(1);

            reader.Read().ShouldBe(true);
            reader.PropertyName.ShouldBe("Authorization");
            reader.ValueType.ShouldBe(ContentType.String);
            var authHeader = string.Concat("Basic ", Convert.ToBase64String(Encoding.UTF8.GetBytes("wiki:pedia")));
            reader.StringValue.ShouldBe(authHeader);


            reader.Read().ShouldBe(true);
            reader.ReadState.ShouldBe(ReadState.Body);

            //read body
            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.Struct);
            reader.ItemCount.ShouldBe(2);

            reader.Read().ShouldBe(true);
            reader.PropertyName.ShouldBe("method");
            reader.ValueType.ShouldBe(ContentType.String);
            reader.StringValue.ShouldBe("system.listMethods");

            reader.Read().ShouldBe(true);
            reader.PropertyName.ShouldBe("parameters");
            reader.ValueType.ShouldBe(ContentType.Array);
            reader.ItemCount.ShouldBe(1);

            reader.Read().ShouldBe(true);
            reader.ValueType.ShouldBe(ContentType.String);
            reader.StringValue.ShouldBe("Bla");

            reader.Read().ShouldBe(true);
            reader.ReadState.ShouldBe(ReadState.EndOfFile);

            reader.Read().ShouldBe(false);
        }
    }
}
