using System;
using System.Collections.Generic;
using System.Text;
using LibRpws2.EventArgs;
using RpwsServerBridge.Entities;

namespace RpwsBlue.MasterServer.Events
{
    class EventPing
    {
        public static void OnEvent(TestPing data, NetworkPacket p)
        {
            //Send the exact same data back.
            TestPing r = new TestPing
            {
                contents = data.contents
            };

            Program.master_server.SendNotificationPacket(r, LibRpws2.RpwsNetEvent.TestPingEvent);
        }
    }
}
