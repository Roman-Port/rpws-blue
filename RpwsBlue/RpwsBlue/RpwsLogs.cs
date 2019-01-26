using LibRpws;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RpwsBlue
{
    public static class RpwsLogs
    {
        /// <summary>
        /// Logs the entire reply/request, including headers, user, and metadata.
        /// </summary>
        public static void LogEntireRequest(HttpContext e, HttpSession s)
        {
            //Copy response
            MemoryStream responseBody = new MemoryStream();
            e.Response.Body.CopyTo(responseBody);
            responseBody.Position = 0;
            StreamReader sr = new StreamReader(responseBody);
            Console.WriteLine(sr.ReadToEnd());
            sr.Close();
            responseBody.Close();
        }
    }
}
