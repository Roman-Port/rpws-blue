using Newtonsoft.Json;
using RpwsBlue.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using LibRpws;

namespace RpwsBlue.Services.Me
{
    public class ApiMeService
    {
        public static void OnClientRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //Generate the reply.
            MeReply r = new MeReply();
            r.devices = new string[] { };
            r.email = ee.user.email;
            r.id = ee.user.pebbleId;
            r.legacy_id = null;
            r.name = ee.user.name;
            r.roles = new string[] { "regular"};
            //Serialize this and send it.
            Program.QuickWriteToDoc(e, JsonConvert.SerializeObject(r), "application/json");
        }
    }
}
