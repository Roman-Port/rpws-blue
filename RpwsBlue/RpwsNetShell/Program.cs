using RpwsServerBridge.Client;
using RpwsServerBridge.Entities;
using System;
using System.Text;

namespace RpwsNetShell
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Connecting...");

            var creds = new PacketCredentials
            {
                encryption_key = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 },
                hmac_key = new byte[] { 0x01, 0x03, 0x07, 0xa0, 0x03, 0x03, 0x4, 0x03 },
                user_id = "RPWS_SYS".ToCharArray()
            };

            var config = new RpwsServerBridge.NetConfigServer
            {
                ip = "10.0.1.13",
                notification_channel_port = 25569,
                question_channel_port = 25570,
                server_creds = creds,
                users = new PacketCredentials[] { creds }
            };

            RpwsBridgeClient client = RpwsBridgeClient.StartClient(config, out bool connected);

            Console.WriteLine("Ready for input.");

            while(true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("\n");
                string message = Console.ReadLine();
                //Encode and send to the server.
                NetworkPacket p = new NetworkPacket(message, (RequestType)21, RequestStatusCode.Ok);
                NetworkPacket reply = client.SendQuestionGetReplyWait(p);
                //Loop through bytes and display reply.
                foreach (byte b in reply.payload)
                {
                    if (b == 0x11)
                        Console.ForegroundColor = ConsoleColor.White;
                    else if (b == 0x12)
                        Console.ForegroundColor = ConsoleColor.Red;
                    else if (b == 0x13)
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    else if (b == 0x14)
                        Console.ForegroundColor = ConsoleColor.Green;
                    else
                        Console.Write(Encoding.ASCII.GetString(new byte[] { b }));
                }
            }
        }
    }
}
