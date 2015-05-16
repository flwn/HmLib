using HmLib.Binary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shouldly;

namespace HmLib.Tests.Binary
{
    public class HmBitConverterTests
    {

        public void TestDoubleConversion()
        {
            var input = 0.5d;
            var bytesVersion = HmBitConverter.GetBytes(input);

            var floatVersion = HmBitConverter.ToDouble(bytesVersion);

            floatVersion.ShouldBe(input);
        }
    }
}
