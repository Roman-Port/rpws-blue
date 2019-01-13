using RpwsServerBridge.Entities;
using RpwsServerBridge.NetBase;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RpwsServerBridge.Client
{
    public class RpwsBridgeClient : NetBase.NetBase
    {
        /// <summary>
        /// Connection to the notification channel.
        /// </summary>
        public new ServerNetworkClient notification_channel;

        /// <summary>
        /// Connection to the question channel.
        /// </summary>
        public new ServerNetworkClient question_channel;

        /// <summary>
        /// Create a connection and begin.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static RpwsBridgeClient StartClient(NetConfigServer config, out bool connected)
        {
            RpwsBridgeClient client = new RpwsBridgeClient();

            //Set variables
            client.config = config;
            client.name = "CLIENT";

            //Create connections.
            connected = client.ReconnectToServer();

            return client;
        }

        public bool ReconnectToServer()
        {
            //Create IP address
            IPAddress ip = IPAddress.Parse(config.ip);
            notification_channel = CreateConnectionToChannel(new IPEndPoint(ip, config.notification_channel_port), "notification_channel");
            question_channel = CreateConnectionToChannel(new IPEndPoint(ip, config.question_channel_port), "question_channel");
            notification_clients.Add(notification_channel);

            //Handshake channels.
            HandshakeChannel(notification_channel);
            HandshakeChannel(question_channel);

            return true;
        }

        private ServerNetworkClient CreateConnectionToChannel(IPEndPoint endpoint, string name)
        {
            //Create the socket.
            Socket sock = new Socket(endpoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            //Connect
            sock.Connect(endpoint);

            //Check
            if (!sock.Connected)
                throw new Exception("Failed to connect!");

            //Create a client object
            ServerNetworkClient state = new ServerNetworkClient(sock, this, name);

            //Begin listening.
            state.BeginListeningForData();

            //Return the state object.
            return state;
        }

        private void HandshakeChannel(ServerNetworkClient c)
        {
            //Send a handshake request.
            c.StartHandshakeAndWait();
        }

        //Api
        public void SendQuestionGetReply(NetworkPacket p, GetPacketCallback callback)
        {
            //Send
            question_channel.SendQuestionGetReply(p, callback);
        }

        public NetworkPacket SendQuestionGetReplyWait(NetworkPacket p, int timeoutMs = 5000)
        {
            //Send
            return question_channel.SendQuestionGetReplyAndWait(p, timeoutMs);
        }

        public void SendQuestionGetReply(byte[] payload, RequestType type, GetPacketCallback callback, RequestStatusCode status = RequestStatusCode.Ok)
        {
            //Encode and create a packet.
            NetworkPacket p = new NetworkPacket
            {
                payload = payload,
                type = type,
                status = status
            };

            //Send
            SendQuestionGetReply(p, callback);
        }

        /// <summary>
        /// Register this client as a spcial type. This will permit the server to send special questions to this client.
        /// </summary>
        public bool RegisterClient(string name)
        {
            NetworkPacket reply = SendQuestionGetReplyWait(new NetworkPacket()
            {
                payload = Encoding.ASCII.GetBytes(name),
                status = RequestStatusCode.Ok,
                type = RequestType.ClientRegister,

            });
            return reply.payload[0] == 0x01;
        }
    }
}
