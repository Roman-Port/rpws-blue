using LibRpws;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LibRpws2.Entities;
using RpwsBlue.Entities;

namespace RpwsBlue.Services.PublishApi
{
    public static class AppList
    {
        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            if (CorePublishApi.SetRequestHeaders(e))
                return;
            //Make sure this is a get request.
            if (Program.FindRequestMethod(e) != RequestHttpMethod.get)
                throw new RpwsStandardHttpException("Invalid method.");

            //Find all apps by this user.
            var apps = CorePublishApi.collection.Find(x => x.app.developer_id == ee.user.pebbleId).ToArray();
            //Sort these apps by their date.
            Array.Sort(apps, delegate (PebbleAppDbStorage x, PebbleAppDbStorage y) {
                return y.dev.last_edit_time.CompareTo(x.dev.last_edit_time);
            });

            //Find claim requests by this user
            AppstoreClaimRequest[] claims = AppstoreClaimRequest.GetCollection().Find(x => x.userUuid == ee.user.uuid && x.status != AppstoreClaimRequestStatus.Accepted).ToArray();

            //Convert these apps to the data.
            AppListReply[] replies = new AppListReply[apps.Length + claims.Length];
            for(int i = 0; i<claims.Length; i++)
            {
                var c = claims[i];
                int index = i;
                AppstoreApp firstApp = AppstoreApi.GetAppById(c.appIds[0]);
                string name = firstApp.title;
                if (c.appIds.Length > 1)
                    name += $" (+{c.appIds.Length - 1})";
                replies[index] = new AppListReply
                {
                    id = c.uuid,
                    image_url = firstApp.list_image.GetItem()._144x144,
                    name = name,
                    time_ago_string = $"Claim since {new DateTime(c.open_time).ToShortDateString()}",
                    type = 1
                };
            }
            for(int i = 0; i<apps.Length; i++)
            {
                var app = apps[i];
                int index = i + claims.Length;
                replies[index] = new AppListReply
                {
                    id = app.app.id,
                    name = app.app.title,
                    time_ago_string = "A long time ago",
                    image_url = "https://romanport.com/static/Pebble/watchface_placeholder_icon_round.png",
                    type = 0
                };
                //Get image
                try
                {
                    replies[index].image_url = app.app.list_image.GetItem()._144x144;
                }
                catch
                {

                }
                //Find dates
                if (app.dbVersion >= 4) //The date will exist.
                    replies[index].time_ago_string = Program.CompareDates(new DateTime(app.dev.last_edit_time), DateTime.UtcNow);
            }
            //Respond with user
            MeReply r = new MeReply
            {
                apps = replies,
                is_app_dev = ee.user.isAppDev,
                app_dev_name = ee.user.appDevName
            };
            //Respond with the apps.
            Program.QuickWriteJsonToDoc(e, r);
        }

        class AppListReply
        {
            public string name;
            public string id;
            public string time_ago_string;
            public string image_url;
            public int type; //0: App, 1: Pending request
        }

        class MeReply
        {
            public AppListReply[] apps;
            public bool is_app_dev;
            public string app_dev_name;
        }
    }
}
