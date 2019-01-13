using LibRpws;
using LibRpws2.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services.Appstore
{
    /// <summary>
    /// Serves the /v3/appstore/app/{appid}/{action} endpoint.
    /// </summary>
    public class AppstoreAppQuery
    {
        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //Add XMLHTTP headers
            AppstoreSecureHeaders.SetSecureHeaders(e);

            //Split URL and extract data. /v3/appstore/app/{appid}/{action}
            string[] splitUrl = e.Request.Path.ToString().Split('/');
            if (splitUrl.Length < 6)
                throw new RpwsStandardHttpException("URL format is incorrect.");
            string appId = splitUrl[4];
            string action = splitUrl[5];

            //Find the app requested.
            AppstoreApp app = AppstoreApi.GetAppById(appId);
            if (app == null)
                throw new RpwsStandardHttpException("App not found.", false, 404);

            //Find the service
            if (action == "info")
                AppServices.AppInfo.OnRequest(e, ee, app);
            else if (action == "upvote")
                AppServices.AppVotes.OnRequest(e, ee, app, 1);
            else if (action == "downvote")
                AppServices.AppVotes.OnRequest(e, ee, app, 0);
            else
                throw new RpwsStandardHttpException($"Action '{action}' did not exist.");
        }
    }
}
