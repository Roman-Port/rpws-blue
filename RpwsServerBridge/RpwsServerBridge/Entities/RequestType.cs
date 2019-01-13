using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsServerBridge.Entities
{
    public enum RequestType
    {
        //Special
        Answer = 0,
        Ping = 1,
        ClientHello = 2, //For handshaking. Sent first.
        ClientRegister = 3, //Registers the client for special questions.

        //Real requests
        TestRequest = 20

        //Events start at 10000
    }
}
