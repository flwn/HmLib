using System;

namespace HmLib.Binary
{
    public static class HmBitConverter
    {

        public static byte[] GetBytes(int value)
        {
            var results = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(results);
            }
            return results;
        }


        public static byte[] GetBytes(bool value)
        {
            var result = new byte[] { 0 };
            if (value)
            {
                result[0] = 1;
            }
            return result;
        }

        public static byte[] GetBytes(double value)
        {
            
            //adapted from https://github.com/Homegear/HomegearLib.NET/blob/master/HomegearLib.NET/RPC/Encoding/BinaryEncoder.cs#L43
            var temp = Math.Abs(value);
            var exponent = 0;
            if (temp >= double.Epsilon && temp < 0.5)
            {
                while (temp < 0.5d)
                {
                    temp *= 2d;
                    exponent--;
                }
            }
            else
            {
                while (temp >= 1)
                {
                    temp /= 2d;
                    exponent++;
                }
            }

            if (value < 0d)
            {
                temp *= -1d;
            }
            
            var mantissa = (int)Math.Round(temp * (double)0x40000000);
            var result = new byte[8];

            Array.Copy(BitConverter.GetBytes(mantissa), result, 4);
            Array.Copy(BitConverter.GetBytes(exponent), 0, result, 4, 4);
            
            return result;
        }

        public static bool ToBoolean(byte value)
        {
            return value == 1;
        }

        public static bool ToBoolean(byte[]value, int startIndex = 0)
        {
            return BitConverter.ToBoolean(value, startIndex);
        }

        public static int ToInt32(byte[] value, int startIndex = 0)
        {
            if (value == null) { throw new ArgumentNullException("value"); }
            if (startIndex < 0) { throw new ArgumentOutOfRangeException("startIndex"); }
            if (startIndex > value.Length - 4) { throw new ArgumentOutOfRangeException("startIndex"); }

            return ToInt32Internal(value, startIndex);
        }

        private static int ToInt32Internal(byte[] value, int startIndex = 0)
        {

            if (BitConverter.IsLittleEndian)
            {
                //make reversed copy
                value = new byte[4]
                {
                    value[startIndex + 3],
                    value[startIndex + 2],
                    value[startIndex + 1],
                    value[startIndex + 0],
                };
            }

            return BitConverter.ToInt32(value, startIndex);
        }

        public static double ToDouble(byte[] value, int startIndex = 0)
        {
            if (value == null) {throw new ArgumentNullException("value");}
            if(startIndex < 0) {throw new ArgumentOutOfRangeException("startIndex");}
            if (startIndex > value.Length - 8) {throw new ArgumentOutOfRangeException("startIndex");}


            var mantissa = (double)ToInt32Internal(value, startIndex);
            var exponent = (double)ToInt32Internal(value, startIndex + 4);

            var floatValue = mantissa / (double)0x40000000;
            floatValue *= Math.Pow(2, exponent);
            return floatValue;
        }
    }
}