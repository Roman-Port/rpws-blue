using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsServerBridge.Entities
{
    /// <summary>
    /// A container class for the HMAC and encryption keys used for security.
    /// </summary>
    public class PacketCredentials
    {
        public char[] user_id;
        public byte[] hmac_key;
        public byte[] encryption_key;
    }
}
