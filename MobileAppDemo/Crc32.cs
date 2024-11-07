using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileAppDemo
{
    public static class Crc32
    {
        private const UInt32 CRC32_POLY = 0x04C11DB7;
        public const UInt32 DEFAULT_CRC32 = 0xFFFFFFFF;
        public const UInt32 DEFAULT_CRC32_XOR = 0xFFFFFFFF;

        public static UInt32 Append(byte[] data, UInt32 init, UInt32 finalXor)
        {
            UInt32 val;
            UInt32 crc = init;

            foreach(byte b in data) 
            {
                val = (crc ^ b) & 0xFF;

                for (int i = 0; i < 8; i++)
                    val = ((val & 1) != 0) ? (val >> 1) ^ CRC32_POLY : val >> 1;

                crc = val ^ crc >> 8;
            }

            return crc ^ finalXor;
        }

        public static UInt32 Append(string msg, UInt32 init, UInt32 finalXor)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(msg);
            return Append(bytes, init, finalXor);
        }
    }
}
