using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsServerBridge.Exceptions
{
    public class PacketEncodeException : Exception
    {
        public string error;

        public PacketEncodeException(string error)
        {
            this.error = error;
        }
    }
}
