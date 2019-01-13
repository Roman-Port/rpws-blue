using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RpwsBlue.Handlers.Errors
{
    public static class HttpErrorHandler
    {
        public static void WriteError(Microsoft.AspNetCore.Http.HttpContext context, string errorName, string errorDescription, int code = 500)
        {
            var response = context.Response;
            response.StatusCode = code;
            response.ContentType = "text/html";

            //Load the template.
            string html = File.ReadAllText(LibRpws.LibRpwsCore.config.contentDir+"Handlers/Errors/errorTemplate.html");
            var data = Encoding.UTF8.GetBytes(html.Replace("%MSG%",errorDescription).Replace("%TITLE%",errorName).Replace("%SERV%",LibRpws.LibRpwsCore.config.server_name));
            response.ContentLength = data.Length;
            response.Body.Write(data, 0, data.Length);
        }
    }
}
