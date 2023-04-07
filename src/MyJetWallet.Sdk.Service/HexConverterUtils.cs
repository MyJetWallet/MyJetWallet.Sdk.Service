using System.Collections.Generic;

namespace MyJetWallet.Sdk.Service
{
    public static class HexConverterUtils
    {
        private static readonly char[] Digits = new char[16]
        {
      '0',
      '1',
      '2',
      '3',
      '4',
      '5',
      '6',
      '7',
      '8',
      '9',
      'A',
      'B',
      'C',
      'D',
      'E',
      'F'
        };
        private static readonly Dictionary<char, byte> FirstByte = new Dictionary<char, byte>
        {
            ['0'] = 0,
            ['1'] = 16,
            ['2'] = 32,
            ['3'] = 48,
            ['4'] = 64,
            ['5'] = 80,
            ['6'] = 96,
            ['7'] = 112,
            ['8'] = 128,
            ['9'] = 144,
            ['A'] = 160,
            ['B'] = 176,
            ['C'] = 192,
            ['D'] = 208,
            ['E'] = 224,
            ['F'] = 240
        };
        private static readonly Dictionary<char, byte> SecondByte = new Dictionary<char, byte>
        {
            ['0'] = 0,
            ['1'] = 1,
            ['2'] = 2,
            ['3'] = 3,
            ['4'] = 4,
            ['5'] = 5,
            ['6'] = 6,
            ['7'] = 7,
            ['8'] = 8,
            ['9'] = 9,
            ['A'] = 10,
            ['B'] = 11,
            ['C'] = 12,
            ['D'] = 13,
            ['E'] = 14,
            ['F'] = 15
        };

        public static string ToHexString(this byte[] bytes)
        {
            char[] chArray1 = new char[bytes.Length * 2];
            int num1 = 0;
            foreach (byte num2 in bytes)
            {
                char[] chArray2 = chArray1;
                int index1 = num1;
                int num3 = index1 + 1;
                int digit1 = Digits[num2 >> 4];
                chArray2[index1] = (char)digit1;
                char[] chArray3 = chArray1;
                int index2 = num3;
                num1 = index2 + 1;
                int digit2 = Digits[num2 & 15];
                chArray3[index2] = (char)digit2;
            }
            return new string(chArray1);
        }

        public static byte[] HexStringToByteArray(this string hexString)
        {
            int length = hexString.Length / 2;
            int index1 = 0;
            int index2 = 0;
            byte[] numArray = new byte[length];
            while (index1 < hexString.Length)
            {
                byte num1 = FirstByte[hexString[index1]];
                int index3 = index1 + 1;
                byte num2 = SecondByte[hexString[index3]];
                index1 = index3 + 1;
                numArray[index2] = (byte)(num1 + (uint)num2);
                ++index2;
            }
            return numArray;
        }
    }
}