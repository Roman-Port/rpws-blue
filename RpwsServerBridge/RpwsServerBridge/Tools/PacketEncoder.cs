using RpwsServerBridge.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RpwsServerBridge.Tools
{
    public static class PacketEncoder
    {
        public const ushort PROTOCOL_VERSION = 1;

        /// <summary>
        /// Create the standard net packet data.
        /// </summary>
        public static byte[] EncodePacket(NetworkPacket p, PacketCredentials creds, ulong secureRequestId)
        {
            return EncodePacket(p.status, p.request_id, p.type, p.payload, creds, secureRequestId);
        }

        /// <summary>
        /// Create the standard net packet data.
        /// </summary>
        public static byte[] EncodePacket(RequestStatusCode status, uint requestId, RequestType type, byte[] payload, PacketCredentials creds, ulong secureRequestId)
        {
            //Create the secure region of the packet.
            byte[] encrypted_payload;
            using (MemoryStream ms = new MemoryStream())
            {
                //Add the status code.
                ByteCoder.WriteUShort((ushort)status, ms);
                //Write request ID
                ByteCoder.WriteUInt(requestId, ms);
                //Write the content type
                ByteCoder.WriteUInt((uint)type, ms);
                //Write the payload size
                ByteCoder.WriteUInt((uint)payload.Length, ms);
                //Copy the payload
                ByteCoder.WriteByteArray(payload, ms);

                //Encrypt the data.
                encrypted_payload = EncryptData(ms, creds.encryption_key);
            }
            //Create the HMAC data.
            byte[] hmac = PacketHMAC.EncodeHmac(new PacketHMACData(creds, encrypted_payload, (uint)encrypted_payload.Length, secureRequestId));
            //Open a stream and begin creating the real packet.
            byte[] output;
            using (MemoryStream ms = new MemoryStream())
            {
                //Encode the user ID
                ByteCoder.WriteCharArray(creds.user_id, ms);
                //Write the protocol version
                ByteCoder.WriteUShort(PROTOCOL_VERSION, ms);
                //Write the size of the encrypted payload.
                ByteCoder.WriteUInt((uint)encrypted_payload.Length, ms);
                //Write the secure request ID to prevent replay attacks.
                ByteCoder.WriteULong(secureRequestId, ms);
                //Check the hmac size
                if (hmac.Length != 32)
                    throw new Exceptions.PacketEncodeException($"HMAC size did not equal 32 bytes. Instead, it was {hmac.Length}.");
                //Write the actual HMAC
                ByteCoder.WriteByteArray(hmac, ms);
                //Finally, write the actual payload.
                ByteCoder.WriteByteArray(encrypted_payload, ms);
                //And then copy it to an output array.
                ms.Position = 0;
                output = ByteCoder.ReadByteArray((int)ms.Length, ms);
            }
            return output;
        }

        private static byte[] EncryptData(Stream content, byte[] key)
        {
            content.Position = 0;
            using (AesCryptoServiceProvider csp = new AesCryptoServiceProvider())
            {
                csp.KeySize = 256;
                csp.BlockSize = 128;
                csp.Key = key;
                csp.Padding = PaddingMode.PKCS7;
                csp.Mode = CipherMode.ECB;
                ICryptoTransform encrypter = csp.CreateEncryptor();
                return encrypter.TransformFinalBlock(ByteCoder.ReadByteArray((int)content.Length, content), 0, (int)content.Length);
            }
        }
    }
}
