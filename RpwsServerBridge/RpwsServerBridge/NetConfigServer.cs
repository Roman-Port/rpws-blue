using RpwsServerBridge.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsServerBridge
{
    public class NetConfigServer
    {
        public PacketCredentials[] users;

        public PacketCredentials server_creds;

        public RpwsLogLevel minimumLogLevel;

        public string ip;

        public int notification_channel_port;
        public int question_channel_port;
    }
}
