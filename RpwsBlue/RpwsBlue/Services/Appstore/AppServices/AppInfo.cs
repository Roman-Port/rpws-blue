using LibRpws;
using LibRpws2.Entities;
using RpwsBlue.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services.Appstore.AppServices
{
    public static class AppInfo
    {
        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee, AppstoreApp a)
        {
            //Get hardware
            string hardware = "basalt";
            if(e.Request.Query.ContainsKey("hardware"))
                hardware = e.Request.Query["hardware"];

            //Convert the Appstore app to the appstore converted app.
            var converted = new AppstoreConvertedUserApp(a, ee, hardware);

            //Now write this to the client.
            Program.QuickWriteJsonToDoc(e, converted);
        }
    }
}
