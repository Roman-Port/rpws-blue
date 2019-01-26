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

            //Fetch new locker apps
            var installed_apps = Services.Locker.LockerTool.GetInstalledApps(ee.user.uuid);
            List<string> installed_apps_list = new List<string>();
            foreach(var a in installed_apps)
            {
                installed_apps_list.Add(a.app_id);
            }
            u.added_ids = installed_apps_list.ToArray();
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
