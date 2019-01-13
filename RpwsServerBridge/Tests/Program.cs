using RpwsServerBridge.Entities;
using RpwsServerBridge.Server;
using RpwsServerBridge.Client;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
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

            /*byte[] data = RpwsServerBridge.Tools.PacketEncoder.EncodePacket(RpwsServerBridge.Entities.RequestStatusCode.Ok, 0, RpwsServerBridge.Entities.RequestType.TestRequest, payload, creds, 2);
            System.IO.File.WriteAllBytes(@"E:\e_test.bin", data);
            Console.ReadLine();
            data = System.IO.File.ReadAllBytes(@"E:\e_test.bin");

            NetworkPacket packet = RpwsServerBridge.Tools.PacketDecoder.SecureDecodeFullPacket(data, 2, new PacketCredentials[] { creds });
            Console.WriteLine(Encoding.UTF8.GetString(packet.payload));
            Console.ReadLine();*/

            //Start listening server.
            /*Console.WriteLine("Starting server...");
            RpwsBridgeServer server = RpwsBridgeServer.StartServer(config);
            Console.WriteLine("Server started!\n");

            Thread.Sleep(500);*/

            //Start client.
            Console.WriteLine("Starting client...");
            RpwsBridgeClient client = RpwsBridgeClient.StartClient(config, out bool connected);
            Console.WriteLine($"Client started and connected={connected.ToString()}!\n");

            //Run tests.
            Console.WriteLine("Running tests...");

            /*Parallel.For(0, 99999, (int i) =>
            {
                NetworkPacket reply = client.question_channel.SendQuestionGetReplyAndWait(new NetworkPacket("Ping test", RequestType.Ping, RequestStatusCode.Ok));
                if (reply.DecodeAsString() != "Ping test")
                    throw new Exception("Failed!");
            });*/

            Console.WriteLine("Done.");
            Console.ReadLine();
        }
    }
}
