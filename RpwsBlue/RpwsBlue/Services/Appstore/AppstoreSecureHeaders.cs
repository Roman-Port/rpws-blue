using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services.Appstore
{
    public static class AppstoreSecureHeaders
    {
        /// <summary>
        /// Set headers for the appstore.
        /// </summary>
        public static void SetSecureHeaders(Microsoft.AspNetCore.Http.HttpContext e)
        {
            if (e.Request.Headers.ContainsKey("origin"))
            {
                //We know the origin. Check if it is from a secure place.
                string origin = e.Request.Headers["origin"];
                if(origin == "https://apps.get-rpws.com")
                {
                    //Secure origin. Allow creds.
                    e.Response.Headers.Add("Access-Control-Allow-Origin", origin);
                    e.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                    e.Response.Headers.Add("Access-Control-Allow-Headers", "authorization");
                } else
                {
                    //Not secure. Don't allow creds
                    e.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
            } else
            {
                //Allow from anywhere, but no creds
                e.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            }
        }
    }
}
