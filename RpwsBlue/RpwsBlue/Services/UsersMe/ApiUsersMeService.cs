using Newtonsoft.Json;
using RpwsBlue.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using LibRpws;


namespace RpwsBlue.Services.UsersMe
{
    public class ApiUsersMeService
    {
        public static void OnClientRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //Generate the reply.
            UsersMeReply r = new UsersMeReply();
            UsersMeReplyUser u = new UsersMeReplyUser();
            u.added_ids = ee.user.lockerInstalled;
            u.applications = new string[] { };
            u.flagged_ids = new string[] { };
            u.href = "https://"+LibRpwsCore.config.public_host+"/v1/usersme/";
            u.id = ee.user.pebbleId;
            u.name = ee.user.name;
            u.uid = ee.user.uuid;
            u.voted_ids = ee.user.likedApps;
            r.users = new UsersMeReplyUser[] { u };
            r.applications = new string[] { };
            //Serialize this and send it.
            Program.QuickWriteToDoc(e, JsonConvert.SerializeObject(r), "application/json");
        }
    }
}
