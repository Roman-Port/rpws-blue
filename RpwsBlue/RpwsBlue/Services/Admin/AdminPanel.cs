using LibRpws;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services.Admin
{
    public static class AdminPanel
    {
        public static void OnSigninRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //Take in params from OAUTH.
            string grant_token = ee.GET["grant_token"];

            //Authenticate and obtain a token.
            var token = RpwsBlue.Services.OauthV2.OauthV2.InternalFinishGrant(grant_token);

            if (token.ok == false)
            {
                //Report failure.
                Program.QuickWriteToDoc(e, "<html><head><title>Oops...</title></head><body><h1>Sorry...</h1><p>Sorry, that grant token was invalid. Authentication failed or it expired. Please try again or <a href=\"https://get-rpws.com/support\" target=\"_blank\">contact support</a>. Thanks!</p></body></html>", "text/html", 500);
                return;
            }

            //Send a token.
            e.Response.Cookies.Append("access-token", token.access_token, new Microsoft.AspNetCore.Http.CookieOptions
            {
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None,
                IsEssential = true,
                Path = "/admin/"
            });

            //Redirect
            LibRpwsCore.Redirect(e, "/admin/");
        }

        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //Validate to see if we're actually authorized
            if(ee.user == null)
            {
                //Not signed in. Redirect to signin.
                string url = $"https://{Program.config.public_host}/v1/oauth2/?returnuri=https%3a%2f%2f{Program.config.public_host}%2fadmin%2fsignin%2f&name=RPWS+Admin&permissions=0";
                LibRpwsCore.Redirect(e, url);
                return;
            }
            if(ee.user.uuid != "mKVEQvNg9JqEFFrt4SqQfD9uM7KqAMXWxSBonoEx75bNKDgPwGFLkOf6W0xxMuuk")
            {
                Program.QuickWriteToDoc(e, "sorry, you can't access this page");
                return;
            }
            //Now, output the header.
            string output = "<html><head><title>RPWS Admin</title><style>th {text-align:left;} td {text-align:left;} .hidden{pointer-events:none; display:none;}</style></head><body style=\"padding:0;margin:0;\"><div style=\"background-color:#e1e1e1; padding:8px; margin-bottom:5px;\">";
            output += "<b>RPWS Admin Console</b> | ";
            //Add services
            output += "<a href=\"/admin/?service=apps\">Apps</a> ";
            output += "<a href=\"/admin/?service=users\">Users</a> ";
            output += "<a href=\"/admin/?service=tokens\">Tokens</a> ";
            output += "| ";
            output += "<a href=\"/admin/?service=logs&type=Request\">Requests</a> ";
            output += "<a href=\"/admin/?service=logs&type=Error\">Errors</a> ";
            output += "<a href=\"/admin/?service=logs&type=Message\">Logs</a> ";
            output += "| ";
            output += "<a href=\"/admin/?service=stats\">Stats</a> ";
            output += "| ";
            output += "<a href=\"/admin/?service=config\">System Config</a> ";
            output += "| ";
            output += "<a href=\"/admin/?service=db_backups\">Database Backups</a> ";
            //End and choose service
            string service = "";
            output += "</div>";
            if (e.Request.Query.ContainsKey("service"))
                service = e.Request.Query["service"];
            //Switch
            string typeOutput;
            switch(service.ToLower())
            {
                case "":
                    typeOutput = "";
                    break;
                case "apps":
                    typeOutput = AdminApps.OnReq(e, ee);
                    break;
                case "users":
                    typeOutput = AdminAccounts.OnReq(e, ee);
                    break;
                case "logs":
                    typeOutput = AdminLogs.OnReq(e, ee);
                    break;
                case "app_migrate":
                    typeOutput = AppMigrate.OnReq(e, ee);
                    break;
                case "stats":
                    typeOutput = AdminHitStats.OnReq(e, ee);
                    break;
                case "config":
                    typeOutput = AdminConfig.OnReq(e, ee);
                    break;
                case "db_backups":
                    typeOutput = AdminBackups.OnReq(e, ee);
                    break;
                default:
                    typeOutput = "bad service";
                    break;
            }
            if(typeOutput == null)
            {
                //Let the other function handle it.
                return;
            } else
            {
                //Continue normally
                output += typeOutput;
            }
            //Finish and send
            output += "</body></html>";
            Program.QuickWriteToDoc(e, output);
        }
    }
}
