//OLD AND JANKY


/*
using LibRpws;
using LibRpwsDatabase.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace RpwsBlue.Services.Login.Oauth
{
    public static class OauthSignin
    {
        public static void OnStart(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //Go to Google servers.
            //Check if required URL params are here.
            if(!ee.GET.ContainsKey("callback_domain") || !ee.GET.ContainsKey("callback_path"))
            {
                //Doesn't have a required one.
                throw new Exception("Sorry, the link you followed was invalid.");
            }
            //Set cookies.
            e.Response.Cookies.Append("RPWS_Oauth_Callback_Domain", ee.GET["callback_domain"]);
            e.Response.Cookies.Append("RPWS_Oauth_Callback_Path", ee.GET["callback_path"]);
            //Create the template.
            string url = "https://accounts.google.com/o/oauth2/v2/auth?scope=email%20profile&amp;access_type=offline&amp;include_granted_scopes=true&amp;state=state_parameter_passthrough_value&amp;redirect_uri=https%3A%2F%2F"+LibRpwsCore.config.public_host+"%2Foauth%2Fcomplete%2F&amp;response_type=code&amp;client_id=1047184416812-dghs3rupvk4s5vhjhdvcibcdkt019ko3.apps.googleusercontent.com";
            string template = TemplateManager.GetRpwsTemplate("Services/Login/Oauth/template-signin.html", new string[] { "%%URL%%" }, new string[] { url });
            Program.QuickWriteToDoc(e, template);
        }

        public static string RequestAuth(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Request auth from the current page. Get the URL to go back to.
            string url = "https://"+LibRpwsCore.config.public_host+"/oauth/?callback_domain=" + HttpUtility.UrlEncode(e.Request.Host.ToString()) + "&callback_path=" + HttpUtility.UrlEncode(e.Request.Path.ToString());
            return url;
        }

        public static void OnEnd(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //Returned from Google servers
            //Validate
            string token = LoginClientService.FinishGoogleOauth(e, ee, out E_RPWS_User user);
            //Set the main login token.
            Microsoft.AspNetCore.Http.CookieOptions sett = new Microsoft.AspNetCore.Http.CookieOptions();
            sett.Expires = DateTimeOffset.UtcNow.AddYears(8);
            e.Response.Cookies.Append("access-token", token,sett);
            //Add older, legacy, tokens
            e.Response.Cookies.Append("RPWS_Oauth_Pebble_Token", token,sett);
            e.Response.Cookies.Append("RPWS_Oauth_Token", token,sett);
            //Redirect to the new location.
            if(!e.Request.Cookies.ContainsKey("RPWS_Oauth_Callback_Domain") || !e.Request.Cookies.ContainsKey("RPWS_Oauth_Callback_Path"))
            {
                throw new Exception("No callbacks foumd. Do you have cookies disabled?");
            }
            string url = "https://" + e.Request.Cookies["RPWS_Oauth_Callback_Domain"] + e.Request.Cookies["RPWS_Oauth_Callback_Path"].Replace("token_pbl_rpws", token).Replace("token_rpws", token);
            LibRpwsCore.Redirect(e,url);
        }
    }
}
*/