using RpwsServerBridge.Exceptions;
using RpwsServerBridge.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace RpwsServerBridge.Tools
{
    public static class PacketDecoder
    {
        /// <summary>
        /// Read a packet in it's full entirety.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="target_secure_request_id"></param>
        /// <param name="user_list"></param>
        /// <returns></returns>
        public static NetworkPacket SecureDecodeFullPacket(byte[] data, ulong target_secure_request_id, PacketCredentials[] user_list, bool skip_secure_request_id_validation)
        {
            //Open the secure region.
            byte[] secure_region = SecureDecodeFullPacket(data, target_secure_request_id, user_list, out PacketCredentials creds, skip_secure_request_id_validation);
            //Now, read this in as a packet.
            return DeserializeSecureRegion(secure_region, creds, target_secure_request_id);
        }

        /// <summary>
        /// Decode a packet and validate it after getting the payload data seperately from the header.
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public static NetworkPacket SecureDecodePacketPayload(byte[] payload, HeaderData header, ulong target_secure_request_id, PacketCredentials[] user_list, out PacketCredentials creds, bool skip_secure_request_id_validation)
        {
            //First, validate the packet.
            if (!SecureValidatePacket(header, payload, target_secure_request_id, user_list, out creds, skip_secure_request_id_validation))
                return null;

            //Decrypt the packet payload.
            byte[] secure_region = DecodeSecureRegion(payload, (int)header.encrypted_size, creds);

            //Now, read the secure region.
            return DeserializeSecureRegion(secure_region, creds, target_secure_request_id);
        }

        private static NetworkPacket DeserializeSecureRegion(byte[] secure_region, PacketCredentials creds, ulong secure_request_id)
        {
            NetworkPacket p = new NetworkPacket();
            p.creds = creds;
            p.secure_request_id = secure_request_id;
            using (MemoryStream ms = new MemoryStream(secure_region))
            {
                p.status = (RequestStatusCode)ByteCoder.ReadUShort(ms);
                p.request_id = ByteCoder.ReadUInt(ms);
                p.type = (RequestType)ByteCoder.ReadUInt(ms);
                uint payload_size = ByteCoder.ReadUInt(ms);
                //Finally, read the content.
                p.payload = ByteCoder.ReadByteArray((int)payload_size, ms);
            }
            return p;
        }

        private static byte[] DecodeSecureRegion(byte[] payload, int encrypted_size, PacketCredentials creds)
        {
            byte[] output;
            using (AesCryptoServiceProvider csp = new AesCryptoServiceProvider())
            {
                csp.KeySize = 256;
                csp.BlockSize = 128;
                csp.Key = creds.encryption_key;
                csp.Padding = PaddingMode.PKCS7;
                csp.Mode = CipherMode.ECB;
                ICryptoTransform decrypter = csp.CreateDecryptor();
                output = decrypter.TransformFinalBlock(payload, 0, encrypted_size);
            }
            return output;
        }

        private static bool CompareArray<T>(T[] a, T[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
                if (!a[i].Equals(b[i]))
                    return false;
            return true;
        }

        private static bool SecureValidatePacket(HeaderData header, byte[] payload, ulong target_secure_request_id, PacketCredentials[] user_list, out PacketCredentials creds, bool skip_secure_request_id_validation)
        {
            //Get the current user.
            creds = null;
            foreach (var u in user_list)
                if (CompareArray(u.user_id, header.user_id))
                    creds = u;
            //Validate
            if (creds == null)
                throw new PacketDecodeException($"The requested user, {header.user_id.ToString()}, didn't exist on this server.");
            //Validate target request ID
            if (target_secure_request_id != header.secure_request_id && skip_secure_request_id_validation == false)
                throw new PacketDecodeException($"Secure request ID received, {header.secure_request_id}, did not equal the expected value, {target_secure_request_id}.");
            //Calculate the hmac to validate results.
            byte[] calculated_hmac_bytes = PacketHMAC.EncodeHmac(new PacketHMACData(creds, payload, header.encrypted_size, header.secure_request_id));
            //Compare
            if (!CompareArray(calculated_hmac_bytes, header.hmac_bytes) && skip_secure_request_id_validation == false)
                throw new PacketDecodeException("***POSSIBLE ATTACK*** HMAC calculated did NOT match the HMAC sent!!");

            return true;
        }

        /// <summary>
        /// Read the packet header and return the secure region. Validate as we go.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="target_secure_request_id"></param>
        /// <param name="user_list"></param>
        /// <param name="creds"></param>
        /// <returns></returns>
        private static byte[] SecureDecodeFullPacket(byte[] data, ulong target_secure_request_id, PacketCredentials[] user_list, out PacketCredentials creds, bool skip_secure_request_id_validation)
        {
            //Read the packet data.
            HeaderData header = ReadMessageHeader(data);

            //Validate payload size
            if (header.encrypted_size > data.Length - header.secure_region_offset)
                throw new PacketDecodeException($"The encrypted content size read, {header.encrypted_size}, was larger than the data sent!");

            //Read the payload.
            byte[] payload = new byte[header.encrypted_size];
            Array.Copy(data, header.secure_region_offset, payload, 0, header.encrypted_size);

            //Validate it.
            if (!SecureValidatePacket(header, payload, target_secure_request_id, user_list, out creds, skip_secure_request_id_validation))
                return null;

            //Now, decrypt the secure region.
            byte[] output = DecodeSecureRegion(payload, (int)header.encrypted_size, creds);
            return output;
        }

        /// <summary>
        /// Read the header without validating anything. Reads up to the payload.
        /// </summary>
        /// <param name="data"></param>
        public static HeaderData ReadMessageHeader(byte[] data)
        {
            HeaderData d = new HeaderData();
            //Open a stream on the content and begin reading.
            using (MemoryStream ms = new MemoryStream(data))
            {
                //Read user ID
                d.user_id = ByteCoder.ReadCharArray(8, ms);
                //Read protocol version
                d.proto_version = ByteCoder.ReadUShort(ms);
                //Validate proto version
                if (d.proto_version != PacketEncoder.PROTOCOL_VERSION)
                    throw new PacketDecodeException($"Protocol version in the request does not match the protocol version this software supports. Request: {d.proto_version} - This: {PacketEncoder.PROTOCOL_VERSION}");
                //Read in the size of the payload.
                d.encrypted_size = ByteCoder.ReadUInt(ms);
                //Read secure request ID
                d.secure_request_id = ByteCoder.ReadULong(ms);
                //Read in the HMAC data
                d.hmac_bytes = ByteCoder.ReadByteArray(32, ms);

                d.secure_region_offset = ms.Position;
            }

            return d;
        }

        
    }

    /// <summary>
    /// The secure packet header.
    /// </summary>
    public class HeaderData
    {
        public char[] user_id;
        public ushort proto_version;
        public uint encrypted_size;
        public ulong secure_request_id;
        public byte[] hmac_bytes;

        /// <summary>
        /// Where the secure region starts. This is the end of the header.
        /// </summary>
        public long secure_region_offset;
    }
}
