using RpwsServerBridge.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RpwsServerBridge.Tools
{
    public static class PacketHMAC
    {
        public static byte[] EncodeHmac(PacketHMACData data)
        {
            //Encode the HMAC for this. First, convert this to bytes.
            byte[] decoded_data;
            using(MemoryStream ms = new MemoryStream())
            {
                ByteCoder.WriteByteArray(data.secret, ms);
                ByteCoder.WriteByteArray(data.sha, ms);
                ByteCoder.WriteUInt(data.length, ms);
                ByteCoder.WriteULong(data.secure_request_id, ms);
                //Copy this to a normal array.
                ms.Position = 0;
                decoded_data = ByteCoder.ReadByteArray((int)ms.Length, ms);
            }
            //Now, produce an HMAC
            HMAC h = new HMACSHA256(data.secret);
            return h.ComputeHash(decoded_data);
        }
    }
    
    public class PacketHMACData
    {
        public byte[] secret;
        public byte[] sha;
        public uint length;
        public ulong secure_request_id;

        public PacketHMACData()
        {

        }

        public PacketHMACData(PacketCredentials pc, byte[] payload, uint length, ulong secure_request_id)
        {
            secret = pc.hmac_key;
            using (SHA256 sha256 = SHA256.Create())
                sha = sha256.ComputeHash(payload);
            this.length = length;
            this.secure_request_id = secure_request_id;
        }
    }
}
