using RpwsServerBridge.Entities;
using RpwsServerBridge.Exceptions;
using RpwsServerBridge.NetBase;
using RpwsServerBridge.Tools;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RpwsServerBridge.Server
{
    public class RpwsBridgeServer : NetBase.NetBase
    {
        

        /// <summary>
        /// Create the server object and begin listening for requests.
        /// </summary>
        /// <returns></returns>
        public static RpwsBridgeServer StartServer(NetConfigServer config)
        {
            RpwsBridgeServer ser = new RpwsBridgeServer();
            //Save user list
            ser.config = config;
            ser.name = "SERVER";
            //Create random
            ser.rand = new Random();
            //Create our socket channels
            IPAddress localAddress = IPAddress.Any;
            ser.notification_channel = ser.CreateChannel(new IPEndPoint(localAddress, config.notification_channel_port), true, "notification_channel");
            ser.question_channel = ser.CreateChannel(new IPEndPoint(localAddress, config.question_channel_port), false, "question_channel");

            return ser;
        }

        /// <summary>
        /// Write to the log.
        /// </summary>
        /// <param name="message"></param>
        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        /// <summary>
        /// Create one of the channels for the socket. Called by the creator.
        /// </summary>
        /// <returns></returns>
        public Socket CreateChannel(IPEndPoint localEndPoint, bool isNotificationChannel, string name)
        {
            //Create the socket.
            Socket listener = new Socket(localEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            //Bind to the address.
            listener.Bind(localEndPoint);
            listener.Listen(50);

            //Create a state object.
            AcceptData ad = new AcceptData
            {
                listener = listener,
                isNotificationChannel = isNotificationChannel,
                name = name
            };

            //Begin listening for reuqests.
            listener.BeginAccept(AcceptCallback, ad);

            //Return the listener.
            return listener;
        }

        class AcceptData
        {
            public Socket listener;
            public bool isNotificationChannel;
            public string name;
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            Log("Incoming connection.");

            //Get the socket and listener.
            AcceptData data = (AcceptData)ar.AsyncState;
            Socket listener = data.listener;
            Socket handler = listener.EndAccept(ar);

            

            //Create a state object.
            ServerNetworkClient state = new ServerNetworkClient(handler, this, data.name);

            //If this is a notification channel, subscribe.
            if (data.isNotificationChannel)
                notification_clients.Add(state);

            //Begin listening on the state
            state.BeginListeningForData();

            //Begin listening again
            listener.BeginAccept(AcceptCallback, data);
        }

        public NetworkPacket SendPacketToRegisteredClient(string name, NetworkPacket message, int timeout = 5000)
        {
            //Seek out a client capable of getting this type of message.
            if (!registered_clients.ContainsKey(name))
                throw new RegisteredClientFindError();

            List<ServerNetworkClient> clients = registered_clients[name];
            Random rand = new Random();

            while(clients.Count > 0)
            {
                //Select a random client for some crappy load balancing. It's pretty bad.
                int index = rand.Next(0, clients.Count);

                //Poll a client to see if we get a reply.
                ServerNetworkClient c = clients[index];

                //Poll it
                bool connected = c.IsConnected();

                //If that passed, try to actually send a message.
                if(connected)
                {
                    try
                    {
                        return c.SendQuestionGetReplyAndWait(message, timeout);
                    }
                    catch
                    {
                        connected = false;
                    }
                }

                //If connected was set to false, boot this client off and try again.
                if(connected == false)
                {
                    c.ForcefulDisconnect();
                }
            }

            //If we land here, all clients failed.
            throw new RegisteredClientFindError();
        }
    }
}
