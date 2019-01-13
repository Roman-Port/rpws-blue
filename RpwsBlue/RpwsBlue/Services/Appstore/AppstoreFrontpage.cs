using LibRpws;
using LibRpws2.Entities;
using Newtonsoft.Json;
using RpwsBlue.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services.Appstore
{
    public static class AppstoreFrontpage
    {
        static Dictionary<string, AppstoreFrontpageCache> cache = new Dictionary<string, AppstoreFrontpageCache>();
        const int CACHE_EXPIRE_TIME_SECONDS = 1800; //30 minutes
        const string ROMAN_USER_ID = "mKVEQvNg9JqEFFrt4SqQfD9uM7KqAMXWxSBonoEx75bNKDgPwGFLkOf6W0xxMuuk";


        class AppstoreFrontpageCache
        {
            public DateTime time;
            public string reply;
        }

        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //Set headers
            AppstoreSecureHeaders.SetSecureHeaders(e);

            //Get type
            string hardware = "basalt";
            if(e.Request.Query.ContainsKey("hardware"))
                hardware = e.Request.Query["hardware"];
            string type = e.Request.Query["t"];
            string name = e.Request.Query["t"]+"/"+hardware;

            //Check if we already have this cached and if it has expired.
            if(cache.ContainsKey(name))
            {
                //Exists. Check if it has expired.
                if ((DateTime.UtcNow - cache[name].time) < new TimeSpan(0, 0, CACHE_EXPIRE_TIME_SECONDS))
                {
                    //Time OK. Respond.
                    Program.QuickWriteToDoc(e, cache[name].reply, "application/json");
                    return;
                }
            }

            //Does not exist in cache. Process...
            lock (cache)
            {
                LibRpwsConfigFile_AppstoreFrontpage data = (LibRpwsConfigFile_AppstoreFrontpage)Program.config.appstore_frontpage[type].Clone();

                //Add auto sections.
                /*if(type == "watchapp")
                {
                    data.sections.Insert(0, new LibRpwsConfigFile_AppstoreFrontpage_Section
                    {
                        title = "All Watchapps",
                    })
                }*/

                //Get Roman's likes
                /*var romanContainsApps = AppstoreVoteApi.GetCollection().Find(x => x.users.ContainsKey(ROMAN_USER_ID));
                LibRpwsConfigFile_AppstoreFrontpage_Section s = new LibRpwsConfigFile_AppstoreFrontpage_Section
                {
                    title = "Featured",
                    actions = new LibRpwsConfigFile_AppstoreFrontpage_Section_Action[0],
                    showAllButton = false,
                    typeId = 1
                };
                List<string> appList = new List<string>();
                foreach (var a in romanContainsApps)
                {
                    AppstoreApp aapp = AppstoreApi.GetAppById(a.appId);
                    
                    if (a.users[ROMAN_USER_ID] == 1 && aapp.type == type)
                    {
                        appList.Add(aapp.id);
                    }
                }
                s.apps = appList.ToArray();
                data.sections.Insert(1,s);*/

                //Generate the sections data.
                FrontpageReply r = new FrontpageReply
                {
                    banners = data.banners.ToArray(),
                    sections = data.sections.ToArray()
                };

                //Find all of the apps and get them.
                Dictionary<string, AppstoreConvertedApp> apps = new Dictionary<string, AppstoreConvertedApp>();

                foreach (var ss in r.sections)
                {
                    foreach (var a in ss.apps)
                    {
                        GetApp(a, ref apps, hardware);
                    }
                }

                //Finish
                r.apps = apps;

                //Convert to JSON and add to cache.
                string json = JsonConvert.SerializeObject(r);
                AppstoreFrontpageCache c = new AppstoreFrontpageCache
                {
                    reply = json,
                    time = DateTime.UtcNow
                };

                if (cache.ContainsKey(name))
                    cache.Remove(name);
                cache.Add(name, c);

                //Respond
                Program.QuickWriteToDoc(e, json, "application/json");
            }
        }

        private static void GetApp(string id, ref Dictionary<string, AppstoreConvertedApp> apps, string hardware)
        {
            if (apps.ContainsKey(id))
                return; //Skip
            AppstoreApp a = AppstoreApi.GetAppById(id);
            if(a != null)
            {
                apps.Add(id, new AppstoreConvertedApp(a, hardware));
            }
        }

        class FrontpageReply
        {
            public LibRpwsConfigFile_AppstoreFrontpage_Section[] sections;
            public LibRpwsConfigFile_AppstoreFrontpage_Banner[] banners;
            public Dictionary<string, AppstoreConvertedApp> apps = new Dictionary<string, AppstoreConvertedApp>();
        }
    }
}
