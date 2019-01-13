using LibRpwsDatabase.Entities;
using Newtonsoft.Json;
using RpwsBlue.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using LibRpws;


namespace RpwsBlue.Services.GetGoing
{
    public class GetGoingPageService
    {
        public static void OnClientRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //First, detect the platform.
            ClientPlatform cp = ClientPlatform.android;
            if(e.Request.Headers.ContainsKey("User-Agent"))
            {
                string userAgent = e.Request.Headers["User-Agent"].ToString();
                if (userAgent.Contains("iPhone") || userAgent.Contains("iPad") || userAgent.Contains("iPod"))
                    cp = ClientPlatform.ios;
            }

            //Get the string name of the branch.
            string branch = "official";
            if(ee.GET.ContainsKey("branch"))
            {
                branch = ee.GET["branch"];
            }

            //Obtain the token and user data by finishing the standard oauth2.
            string token;
            try
            {
                token = RpwsOauth2.RpwsOauth.FinishOauth(ee.GET["endpoint"]);
            } catch
            {
                //Failed to authenticate. Redirect.
                Handlers.Errors.HttpErrorHandler.WriteError(e, "Not Authenticated", "<p>Sorry, you must've reloaded. Please <a href=\"https://get-rpws.com/\">retry here</a>.</p>", 200);
                return;
            }

            //Authenticate ourself and get our user info
            E_RPWS_User user = LibRpws.Auth.LibRpwsAuth.ValidateAccessToken(token, ee);

            if (user == null)
            {
                //This is very, very bad.
                Program.QuickWriteToDoc(e, "A critical issue with RPWS related to authentication has been detected. It should be fixed in the coming hours. I'm sorry. (ERR1)");
                LibRpwsCore.SendCriticalAlert("CRITICAL: Failed to revalidate access token when signing up for RPWS. (ERR1)");
                return;
            }

            string url = "pebble://custom-boot-config-url/https%3A%2F%2Fconfig.pebbleapis.romanport.com%2F"+branch+"%2Fv2%2F"+cp.ToString()+"%2F%3Ftoken%3D"+token;

            string template = TemplateManager.GetTemplate("Services/GetGoing/GetGoingPageTemplate_"+cp.ToString()+".html", new string[] { "%%URL%%", "%%NAME%%" }, new string[] { url, user.name.Split(' ')[0] });
            Program.QuickWriteToDoc(e, template);
        }

        enum ClientPlatform
        {
            android,
            ios
        }
    }
}
