using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RpwsBlue.MasterServer.Shell
{
    class ShellStream
    {
        private MemoryStream ms;

        public ShellStream()
        {
            ms = new MemoryStream();
        }

        /// <summary>
        /// Write an ASCII string.
        /// </summary>
        /// <param name="message"></param>
        public void WriteText(string message)
        {
            byte[] buf = Encoding.ASCII.GetBytes(message);
            ms.Write(buf, 0, buf.Length);
        }

        /// <summary>
        /// Change the console color by sending custom ASCII bytes.
        /// </summary>
        public void ChangeColor(NetShellColor color)
        {
            ms.WriteByte((byte)color);
        }

        /// <summary>
        /// Convert this to a final array.
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            byte[] buf = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(buf, 0, buf.Length);

            return buf;
        }

        public void WriteTableEntry(string message, int len)
        {
            if (message.Length > len)
            {
                message = message.Substring(0, len - 3);
                message += "...";
            }
            while (message.Length < len)
                message += " ";
            message += "| ";
            WriteText(message);
        }
    }

    enum NetShellColor
    {
        White = 0x11,
        Red = 0x12,
        Yellow = 0x13,
        Green = 0x14
    }
}
