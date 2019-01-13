using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RpwsServerBridge.Tools
{
    /// <summary>
    /// Encodes/decodes basic types into or out of streams.
    /// </summary>
    public static class ByteCoder
    {
        public const bool IS_LITTLE_ENDIAN = true;

        private static void WriteToStreamContent(byte[] data, Stream s)
        {
            //Respect endians
            if (BitConverter.IsLittleEndian != IS_LITTLE_ENDIAN)
                Array.Reverse(data);
            //Write
            s.Write(data, 0, data.Length);
        }

        private static byte[] ReadBytesFromStream(int length, Stream s)
        {
            //Read
            byte[] buf = new byte[length];
            s.Read(buf, 0, length);
            //Respect endians
            if (BitConverter.IsLittleEndian != IS_LITTLE_ENDIAN)
                Array.Reverse(buf);
            return buf;
        }

        //Writing

        public static void WriteUInt(UInt32 v, Stream s)
        {
            WriteToStreamContent(BitConverter.GetBytes(v), s);
        }

        public static void WriteUShort(UInt16 v, Stream s)
        {
            WriteToStreamContent(BitConverter.GetBytes(v), s);
        }

        public static void WriteULong(UInt64 v, Stream s)
        {
            WriteToStreamContent(BitConverter.GetBytes(v), s);
        }

        public static void WriteByteArray(byte[] content, Stream s)
        {
            s.Write(content, 0, content.Length);
        }

        public static void WriteCharArray(char[] data, Stream s)
        {
            WriteByteArray(Encoding.ASCII.GetBytes(data), s);
        }

        //Reading

        public static UInt64 ReadULong(Stream s)
        {
            return BitConverter.ToUInt64(ReadBytesFromStream(8, s), 0);
        }

        public static UInt32 ReadUInt(Stream s)
        {
            return BitConverter.ToUInt32(ReadBytesFromStream(4, s), 0);
        }

        public static UInt16 ReadUShort(Stream s)
        {
            return BitConverter.ToUInt16(ReadBytesFromStream(2, s), 0);
        }

        public static byte[] ReadByteArray(int length, Stream s)
        {
            byte[] content = new byte[length];
            s.Read(content, 0, length);
            return content;
        }

        public static char[] ReadCharArray(int length, Stream s)
        {
            return Encoding.ASCII.GetChars(ReadByteArray(length, s));
        }
    }
}
