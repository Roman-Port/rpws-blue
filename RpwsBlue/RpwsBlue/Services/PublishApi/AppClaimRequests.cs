using LibRpws;
using LibRpws2.Entities;
using Newtonsoft.Json;
using RpwsBlue.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RpwsBlue.Services.PublishApi
{
    public static class AppClaimRequests
    {
        public static void OnBeginRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession eee)
        {
            //Just serve claim request page.
            string p = File.ReadAllText(Program.config.contentDir + "Services/PublishApi/claim_template.html").Replace("%%HOST%%", Program.config.public_host);
            Program.QuickWriteToDoc(e, p);
        }

        

        public static void OnEndRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession eee)
        {
            //Read form data
            string[] claimType = e.Request.Form["claimType"].ToArray();
            string[] appIds = e.Request.Form["appId"].ToString().Split(' ');
            string claimText = e.Request.Form["claimText"].ToString();

            //Convert to object
            AppstoreClaimRequest request = new AppstoreClaimRequest
            {
                appIds = appIds,
                open_time = DateTime.UtcNow.Ticks,
                reasoning = claimText,
                status = AppstoreClaimRequestStatus.Open,
                userUuid = eee.user.uuid,
                uuid = DateTime.UtcNow.Ticks.ToString()+LibRpwsCore.GenerateRandomString(8)
            };

            List<AppstoreClaimRequestReasons> claimTypes = new List<AppstoreClaimRequestReasons>();
            foreach(string s in claimType)
            {
                claimTypes.Add((AppstoreClaimRequestReasons)int.Parse(s));
            }
            request.claimTypes = claimTypes.ToArray();

            //Validate apps
            List<AppstoreApp> apps = new List<AppstoreApp>();
            foreach(string s in appIds)
            {
                var app = AppstoreApi.GetAppById(s);
                apps.Add(app);
                if (app == null)
                {
                    SendCommandToOuterFrame(e, new Dictionary<string, string>
                    {
                        {"msg",$"App '{s}' didn't exist. Make sure apps are split by spaces." }
                    }, "fail");
                    return;
                }
            }

            //Insert into list.
            AppstoreClaimRequest.GetCollection().Insert(request);

            //Send myself a notification.
            request.SendWebmasterEmail("New Request", "A new app claim request was opened.");

            string appNames = "";
            foreach (var a in apps)
                appNames += $"* {a.title} ({a.id}) - {a.author}\n";
            request.SendClientEmail("You Opened a Request", $"Hi there,\n\nYou opened a request for the following apps on RPWS:\n\n{appNames}\nYou should hear back here in the coming days. You may view your request here. https://publish.get-rpws.com/request/{request.uuid}/. Thank you so much!");

            //Ok
            SendCommandToOuterFrame(e, new Dictionary<string, string>
            {
                {"msg",$"Submitted!" },
                {"uuid", request.uuid }
            }, "ok");
        }

        private static void SendCommandToOuterFrame(Microsoft.AspNetCore.Http.HttpContext e, Dictionary<string, string> data, string type)
        {
            data.Add("type", type);
            Program.QuickWriteToDoc(e, $"<html><head><title>App Message</title></head><body><script>var payload='{JsonConvert.SerializeObject(data).Replace("'", @"\'")}'; window.top.postMessage(payload, '*'); console.log(payload);</script></body></html>");
        }
    }
}
