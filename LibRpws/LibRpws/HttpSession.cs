using LibRpwsDatabase.Entities;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace LibRpws
{
    public class HttpSession
    {
        public HttpSession(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Get query.
            foreach (var q in e.Request.Query)
            {
                GET.Add(q.Key, q.Value);
            }
            //Connect to the database server.
            context = e;
            //Try to log into the user if there is a Bearer token.
            if (e.Request.Headers.ContainsKey("authorization"))
            {
                string token = e.Request.Headers["authorization"].ToString();
                if (token.StartsWith("Bearer "))
                {
                    token = token.Substring("Bearer ".Length);
                }
                //Try to login.
                user = LibRpws.Auth.LibRpwsAuth.ValidateAccessToken(token, this);
                this.token = token;
            }
            //MAybe the token is passed via the get param.
            if (user == null && GET.ContainsKey("token"))
            {
                user = LibRpws.Auth.LibRpwsAuth.ValidateAccessToken(GET["token"], this);
                token = GET["token"];
            }
            //Perhaps it was a cookie?
            if(user == null && context.Request.Cookies.ContainsKey("access-token"))
            {
                user = LibRpws.Auth.LibRpwsAuth.ValidateAccessToken(context.Request.Cookies["access-token"], this);
                token = context.Request.Cookies["access-token"];
            }
            //Perhaps it was the other cookie?
            if (user == null && context.Request.Cookies.ContainsKey("access_token"))
            {
                user = LibRpws.Auth.LibRpwsAuth.ValidateAccessToken(context.Request.Cookies["access_token"], this);
                token = context.Request.Cookies["access_token"];
            }
        }

        public HttpSession()
        {
        }

        public bool CheckIfAllExistInGetParams(string[] keys)
        {
            foreach (string k in keys)
                if (!GET.ContainsKey(k))
                    return false;
            return true;
        }

        public Dictionary<string, string> GET = new Dictionary<string, string>();
        public Dictionary<string, string> POST = new Dictionary<string, string>();

        public readonly Microsoft.AspNetCore.Http.HttpContext context;
        public string token = null;

        public E_RPWS_User user = null;
    }
}
