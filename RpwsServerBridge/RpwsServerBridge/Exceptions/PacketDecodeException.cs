using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsServerBridge.Exceptions
{
    public class PacketDecodeException : Exception
    {
        public string error;

        public PacketDecodeException(string error)
        {
            this.error = error;
        }
    }
}
