using RpwsServerBridge.Entities;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace RpwsServerBridge.NetBase
{
    /// <summary>
    /// Handles basic networking goodness.
    /// </summary>
    public class NetBase
    {
        public Socket notification_channel;
        public Socket question_channel;

        public Random rand;
        public NetConfigServer config;

        public string name;

        public List<ServerNetworkClient> notification_clients = new List<ServerNetworkClient>();

        /// <summary>
        /// Registered clients for special commands.
        /// </summary>
        public Dictionary<string, List<ServerNetworkClient>> registered_clients = new Dictionary<string, List<ServerNetworkClient>>();

        /// <summary>
        /// Write to the log for an error.
        /// </summary>
        /// <param name="message"></param>
        public void ErrorLog(string message, Exception ex)
        {
            Log($"{message}: {ex.Message} at {ex.StackTrace}", RpwsLogLevel.Error);
        }

        /// <summary>
        /// Write to the log.
        /// </summary>
        /// <param name="message"></param>
        public void Log(string message, RpwsLogLevel logLevel = RpwsLogLevel.Normal, string extraHeader="")
        {
            if (extraHeader.Length > 1)
                extraHeader = "/" + extraHeader;
            if((int)config.minimumLogLevel >= (int)logLevel)
                Console.WriteLine($"[{name}/{logLevel.ToString().ToUpper()}{extraHeader}] "+message);
        }

        /// <summary>
        /// Checks if either channel is disconnected. Thanks to https://stackoverflow.com/questions/722240/instantly-detect-client-disconnection-from-server-socket
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public bool IsConnected()
        {
            try
            {
                return !(notification_channel.Poll(1, SelectMode.SelectRead) && notification_channel.Available == 0) && !(question_channel.Poll(1, SelectMode.SelectRead) && question_channel.Available == 0);
            }
            catch (SocketException) { return false; }
        }

        /// <summary>
        /// Send a notification packet to all clients.
        /// </summary>
        /// <param name="packet"></param>
        public virtual void SendNotificationPacket(NetworkPacket packet)
        {
            //Loop through all of the clients and encode their packet.
            foreach (var client in notification_clients)
            {
                client.SendPacket(packet);
            }
        }

        /// <summary>
        /// Called on a notification packet being received.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="client"></param>
        public virtual void OnNotificationMessage(NetworkPacket packet, ServerNetworkClient client)
        {
            Log("Got full notification packet, but had no actions to take.");
        }

        /// <summary>
        /// Called on a question packet being received.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="client"></param>
        public virtual void OnQuestionMessage(NetworkPacket packet, ServerNetworkClient client)
        {
            Log("Got full question packet, but had no actions to take.");
        }
    }
}
