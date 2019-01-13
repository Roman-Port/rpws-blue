using LibRpws;
using LibRpwsDatabase.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services
{
    public class RpwsMe
    {
        public static void OnClientRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //Create reply
            RpwsMeReply reply = new RpwsMeReply();
            reply.user = ee.user;
            Program.QuickWriteJsonToDoc(e, reply);
        }
    }

    class RpwsMeReply
    {
        public E_RPWS_User user;
    }
}
