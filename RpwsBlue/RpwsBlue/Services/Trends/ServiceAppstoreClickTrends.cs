using LibRpws;
using LibRpws.Trends;
using Newtonsoft.Json;
using RpwsBlue.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services.Trends
{
    public class ServiceAppstoreClickTrends
    {
        public static void GetTrends(Microsoft.AspNetCore.Http.HttpContext context, HttpSession ee)
        {
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            TrendsReply reply = new TrendsReply();
            reply.search_terms = TrendApi.GetRecentTrendObjects(ee, GetType(ee));
            Program.QuickWriteToDoc(context, JsonConvert.SerializeObject(reply),"application/json");
        }

        public static void PutTrends(Microsoft.AspNetCore.Http.HttpContext context, HttpSession ee)
        {
            Appstore.AppstoreSecureHeaders.SetSecureHeaders(context);
            string id = "guest_" + ee.GET["token"];
            if(ee.user != null)
            {
                id = ee.user.uuid;
            }
            TrendApi.CountTrend(ee, GetType(ee), ee.GET["id"], id);
            Program.QuickWriteToDoc(context, "{\"ok\":true}", "application/json");
        }

        private static LibRpwsDatabase.Entities.E_RPWS_TrendType GetType(HttpSession ee)
        {
            if(ee.GET.ContainsKey("type"))
            {
                if (ee.GET["type"] == "watchapp")
                    return LibRpwsDatabase.Entities.E_RPWS_TrendType.AppClickWatchapps;
                else if (ee.GET["type"] == "watchface")
                    return LibRpwsDatabase.Entities.E_RPWS_TrendType.AppClickWatchfaces;
                else
                    throw new Exception("Type offered invalid.");
            } else
            {
                throw new Exception("No type offered.");
            }
        }
    }
}
