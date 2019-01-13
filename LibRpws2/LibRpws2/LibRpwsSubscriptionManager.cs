using System;
using System.Collections.Generic;
using System.Text;
using LibRpws2.EventArgs;
using RpwsServerBridge.Entities;

namespace LibRpws2
{
    public delegate void SubscriptionCallback<T>(T data, NetworkPacket p);

    public static class LibRpwsSubscriptionManager
    {
        //Events themselves

        /// <summary>
        /// Called on the test ping sent.
        /// </summary>
        public static event SubscriptionCallback<TestPing> OnTestPing;

        //Event selector
        public static void RunEvent(NetworkPacket p)
        {
            int id = (int)p.type - 10000;
            switch (id)
            {
                case 1: OnTestPing(p.DecodeAsJson<TestPing>(), p); break;

            }
        }
    }
}
