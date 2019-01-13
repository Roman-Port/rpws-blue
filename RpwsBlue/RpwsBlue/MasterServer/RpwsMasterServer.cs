using System;
using System.Collections.Generic;
using System.Text;
using RpwsServerBridge;
using RpwsServerBridge.Server;
using RpwsServerBridge.Entities;
using System.Net;
using RpwsServerBridge.NetBase;
using LibRpws2;
using Newtonsoft.Json;
using LibRpws2.QuestionArgs;

namespace RpwsBlue.MasterServer
{
    class RpwsMasterServer : RpwsBridgeServer
    {
        /// <summary>
        /// Start the server and begin listening.
        /// </summary>
        public RpwsMasterServer() 
        {
            //This is currently unused. These creds will change later.
            return;

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
                users = new PacketCredentials[] { creds },
                minimumLogLevel = RpwsLogLevel.Debug
            };

            //Open a server and begin listening.
            //Save user list
            this.config = config;
            name = "MASTER SERVER";
            //Create random
            rand = new Random();
            //Create our socket channels
            IPAddress localAddress = IPAddress.Any;
            notification_channel = CreateChannel(new IPEndPoint(localAddress, config.notification_channel_port), true, "notification_channel");
            question_channel = CreateChannel(new IPEndPoint(localAddress, config.question_channel_port), false, "question_channel");

            //Subscribe to events.
            LibRpwsSubscriptionManager.OnTestPing += Events.EventPing.OnEvent;
        }

        public void FireEvent(object data, RpwsNetEvent type)
        {
            //Create a network packet to fire.
            //Convert the type sent here to a type to send over the network.
            int typeId = (int)type + 10000;
            RequestType netType = (RequestType)typeId;

            //Produce a packet.
            NetworkPacket p = new NetworkPacket(data, netType, RequestStatusCode.Ok);

            //Send packet to all clients.
            SendNotificationPacket(p);
        }

        /// <summary>
        /// Senda notification packet to all clients.
        /// </summary>
        /// <param name="packet"></param>
        public void SendNotificationPacket(object payload, RpwsNetEvent type)
        {
            //Create packet
            int typeId = (int)type + 10000;
            NetworkPacket p = new NetworkPacket(payload, (RequestType)typeId, RequestStatusCode.Ok);
            
            //Loop through all of the clients and encode their packet.
            foreach (var client in notification_clients)
            {
                client.SendPacket(p);
            }
        }

        /// <summary>
        /// Called when an event comes in.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="client"></param>
        public override void OnNotificationMessage(NetworkPacket packet, ServerNetworkClient client)
        {
            //Process the event sent.
            Log("Got event " + packet.ToDebugString(), RpwsLogLevel.Normal);
            LibRpwsSubscriptionManager.RunEvent(packet);
        }

        /// <summary>
        /// Called when there are incoming messages.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="client"></param>
        public override void OnQuestionMessage(NetworkPacket packet, ServerNetworkClient client)
        {
            //Get the real request type.
            RpwsNetQuestion type = (RpwsNetQuestion)((int)packet.type - 20);

            Console.WriteLine("Got question of type " + type.ToString());

            switch(type)
            {
                case RpwsNetQuestion.Shell:
                    byte[] payload = Shell.ShellProcessor.ProcessShellCommand(packet.DecodeAsString());
                    packet.ReplyToQuestion(new NetworkPacket(payload, RequestType.Answer, RequestStatusCode.Ok));
                    break;
                case RpwsNetQuestion.ValidateAccessToken: Questions.ValidateAccessToken.OnEvent(packet.DecodeAsJson<ValidateAccessTokenArgs>(), packet); break;
                case RpwsNetQuestion.GetAppById: Questions.MasterServerAppstoreApi.OnEvent_GetAppById(packet.DecodeAsJson<string>(), packet); break;
                case RpwsNetQuestion.GetAppByUUID: Questions.MasterServerAppstoreApi.OnEvent_GetAppByUuid(packet.DecodeAsJson<string>(), packet); break;
                case RpwsNetQuestion.GetAppsByIdIndexed: Questions.MasterServerAppstoreApi.OnEvent_GetAppsByIdIndex(packet.DecodeAsJson<string[]>(), packet); break;
                case RpwsNetQuestion.GetAppsByUUIDIndexed: Questions.MasterServerAppstoreApi.OnEvent_GetAppsByUuidIndex(packet.DecodeAsJson<string[]>(), packet); break;
            }
            
        }

        public NetworkPacket SendPacketToRegisteredClientLibRpws(string name, byte[] message, RpwsNetQuestion questionType, int timeout = 5000)
        {
            //Convert question type to standard type.
            RequestType type = (RequestType)((int)questionType + 20);

            //Create request packet
            NetworkPacket request = new NetworkPacket(message, type, RequestStatusCode.Ok);

            //Send
            return SendPacketToRegisteredClient(name, request, timeout);
        }
    }
}
