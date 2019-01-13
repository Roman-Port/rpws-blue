using LibRpws;
using LibRpwsDatabase.Entities;
using Newtonsoft.Json;
using RpwsBlue.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace RpwsBlue.Services.Login
{
    /// <summary>
    /// Provides the client login prompt with the token passed in through the URL.
    /// </summary>
    class LoginClientService
    {
        public static void OnClientRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //Grab the user and see if the token is valid.
            if(ee.GET.ContainsKey("token"))
            {
                //Try to authenticate.
                E_RPWS_User user = LibRpws.Auth.LibRpwsAuth.ValidateAccessToken(ee.GET["token"], ee);
                if(user != null)
                {
                    string token = ee.GET["token"];
                    string email = user.name + " ("+user.email+")";

                    string template = TemplateManager.GetTemplate("Services/Login/LoginClientTemplate.html", new string[] { "%LOGIN_LINK%", "%EMAIL%" }, new string[] { "/login/client/finish/?token=" + token, email });
                    Program.QuickWriteToDoc(e, template);
                } else
                {
                    //There invalid token. Reject.
                    Program.QuickWriteToDoc(e, "Token sent was invalid. Try again?", "text/plain");
                }
            } else
            {
                //There wasn't a token offered. Reject.
                Program.QuickWriteToDoc(e, "There wasn't a token offered. Try again?", "text/plain");
            }

            
        }

        public static void OnFinishRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //Grab the user and see if the token is valid.
            if (ee.GET.ContainsKey("token"))
            {
                //Try to authenticate.
                E_RPWS_User user = LibRpws.Auth.LibRpwsAuth.ValidateAccessToken(ee.GET["token"],ee);
                if (user != null)
                {
                    string token = ee.GET["token"];
                    string email = user.email;

                    string template = TemplateManager.GetTemplate("Services/Login/LoginClientFinishTemplate.html", new string[] { "%TOKEN%" }, new string[] { token });
                    Program.QuickWriteToDoc(e, template);
                }
                else
                {
                    //There invalid token. Reject.
                    Program.QuickWriteToDoc(e, "Token sent was invalid. Try again?", "text/plain");
                }
            }
            else
            {
                //There wasn't a token offered. Reject.
                Program.QuickWriteToDoc(e, "There wasn't a token offered. Try again?", "text/plain");
            }


        }

        
    }
}
