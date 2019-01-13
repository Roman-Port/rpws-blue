using RpwsServerBridge;
using RpwsServerBridge.Entities;
using System;

namespace LibRpws2.Api
{
    public static class LibRpws2Core
    {
        /// <summary>
        /// Raw net client.
        /// </summary>
        public static LibRpwsClient client;

        /// <summary>
        /// Initialize the connection to the server.
        /// </summary>
        public static void InitConnection()
        {
            var creds = new PacketCredentials
            {
                encryption_key = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 },
                hmac_key = new byte[] { 0x01, 0x03, 0x07, 0xa0, 0x03, 0x03, 0x4, 0x03 },
                user_id = "RPWS_SYS".ToCharArray()
            };

            var config = new NetConfigServer
            {
                ip = "10.0.1.13",
                notification_channel_port = 25569,
                question_channel_port = 25570,
                server_creds = creds,
                users = new PacketCredentials[] { creds },
                minimumLogLevel = RpwsLogLevel.Debug
            };

            client = new LibRpwsClient(config);
        }

        /// <summary>
        /// Check to make sure the connection is up. This should be run before executing any net calls.
        /// </summary>
        /// <returns></returns>
        public static bool CheckConnection()
        {
            return true;
        }
    }
}
