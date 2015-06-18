using HmLib.Binary;
using Shouldly;

namespace HmLib.Tests.Binary
{
    using _Infrastructure;

    public class HmBitConverterTests
    {
        public void GetBytesUsesCorrectEndianess()
        {
            var expected = new byte[] { 0, 0, 0, 0x1 };
            var result = HmBitConverter.GetBytes(0x1);
            result.ShouldBe(expected);
        }

        [Input(0d)]
        [Input(0.16d)]
        [Input(0.5d)]
        [Input(0.95d)]
        [Input(0.0009d)]
        public void TestDoubleConversion(double input)
        {
            var bytesVersion = HmBitConverter.GetBytes(input);

            var floatVersion = HmBitConverter.ToDouble(bytesVersion);

            floatVersion.ShouldBe(input);
        }
    }
}
