using LibRpws2.Analytics;
using LibRpws2.Entities;
using LibRpwsDatabase.Entities;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RpwsBlue.Services.Locker
{
    class LockerService
    {
        public static Task OnRequest(HttpContext e, E_RPWS_User user)
        {
            RpwsAnalytics a = new RpwsAnalytics("Locker", "Auth");

            string user_uuid = user.uuid;

            a.NextCheckpoint("Preparing");

            //Get the type of request.
            RequestHttpMethod method = Program.FindRequestMethod(e);

            //If there is a specific app requested, obtain it.
            AppstoreApp requestedApp = GetAppFromUrl(e);
            Console.WriteLine(requestedApp == null);

            //Handle with and without this and by method.
            if (requestedApp == null)
            {
                //No app requested. 
                switch (method)
                {
                    case RequestHttpMethod.get: return OnGet(e, user_uuid, a);
                }
            }
            else
            {
                //App requested.
                switch (method)
                {
                    case RequestHttpMethod.get: return OnGetWithApp(e, user_uuid, requestedApp, a);
                    case RequestHttpMethod.put: return OnPutWithApp(e, user_uuid, requestedApp, a);
                    case RequestHttpMethod.delete: return OnDeleteWithApp(e, user_uuid, requestedApp, a);
                }
            }

            //Invalid method.
            throw new RpwsStandardHttpException("Invalid Method", "The method, " + method.ToString() + ", is not supported in this state on this service.");
        }

        public static void RpwsOnRequest(HttpContext e, LibRpws.HttpSession session)
        {
            OnRequest(e, session.user);
        }

        /// <summary>
        /// Called when there is a get request to an app. Return the app data.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="a"></param>
        private static Task OnGetWithApp(HttpContext e, string user_uuid, AppstoreApp a, RpwsAnalytics ra)
        {
            //Just respond with this app.
            LockerAppFormat app = LockerTool.ConvertToLockerFormat(a, user_uuid, ra);
            if (app == null)
                throw new RpwsStandardHttpException("Locker Failure", "This app failed to be converted.");

            ra.End();
            ra.DumpToConsole();

            Program.QuickWriteJsonToDoc(e, new LockerFormatting_SingleApp
            {
                application = app
            });
            return null;
        }

        /// <summary>
        /// Called where there is a get request without an app.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="user_uuid"></param>
        /// <returns></returns>
        private static Task OnGet(HttpContext e, string user_uuid, RpwsAnalytics ra)
        {
            //Return the entire list of apps. Get the users' apps, then convert em' all.
            ra.NextCheckpoint("Getting installed apps");

            //Find all tokens
            var app_tokens = LockerTool.GetInstalledApps(user_uuid);

            //Find the number of votes
            ra.NextCheckpoint("Getting votes");
            //To do this, convert app IDs to a string array
            string[] ids = new string[app_tokens.Length];
            for (int i = 0; i < ids.Length; i++)
                ids[i] = app_tokens[i].app_id;
            //Now, get the votes
            int[] votes = AppstoreVoteApi.GetTotals(ids);

            ra.NextCheckpoint("Converting apps");

            //Loop through tokens and get apps.
            List<LockerAppFormat> apps = new List<LockerAppFormat>();
            for(int i = 0; i<app_tokens.Length; i++)
            {
                AppstoreApp orig = AppstoreApi.GetAppByUUID(app_tokens[i].app_uuid);
                if (orig != null)
                {
                    apps.Add(new LockerAppFormat(orig, app_tokens[i].token, votes[i]));
                    try
                    {

                    }
                    catch
                    {
                        //No nothing...
                    }
                }
                ra.NextCheckpointRep();
            }

            ra.End();
            ra.DumpToConsole();

            //Spit this out as a JSON document.
            Program.QuickWriteJsonToDoc(e, new LockerFormatting_MultipleApps
            {
                applications = apps
            });
            return null;
        }

        /// <summary>
        /// Called when a PUT request is sent to an app to install it.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="user_uuid"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        private static Task OnPutWithApp(HttpContext e, string user_uuid, AppstoreApp a, RpwsAnalytics ra)
        {
            ra.NextCheckpoint("Installing app");
            //Install this app.
            LockerTool.InstallApp(a, user_uuid);

            //Respond as if this was a GET request.
            return OnGetWithApp(e, user_uuid, a, ra);
        }

        /// <summary>
        /// Called when a DELETE request is sent to an app to install it.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="user_uuid"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        private static Task OnDeleteWithApp(HttpContext e, string user_uuid, AppstoreApp a, RpwsAnalytics ra)
        {
            ra.NextCheckpoint("Deleting app");
            //Uninstall this app.
            LockerTool.DeleteApp(a, user_uuid);

            //Respond as OK
            Program.QuickWriteToDoc(e, "{}", "application/json");
            return null;
        }

        private static AppstoreApp GetAppFromUrl(HttpContext e)
        {
            //Split the URL to obtain the location where the pathname should be. "/v1/locker/{app id}"
            string[] split = e.Request.Path.ToString().Split('/');

            //Check if the path is long enough
            if (split.Length < 4)
                return null;

            //Get the ID
            string id = split[3];

            //Try to get the app
            return AppstoreApi.GetAppByUUID(id);
        }

        private static List<string> pbw_patched_apps;

        public static bool CheckIfAppIsPbwPatched(string id)
        {
            if (pbw_patched_apps == null)
                pbw_patched_apps = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(@"C:\Users\Roman\tl_pbws\public\db.json"));

            return pbw_patched_apps.Contains(id);
        }
    }
}
