using LibRpws;
using LibRpwsDatabase.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services
{
    public class StatsPage
    {
        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            string html = "<html><head><title>Blue</title><link href=\"https://fonts.googleapis.com/css?family=Open+Sans\" rel=\"stylesheet\"><style>body{font-family: 'Open Sans', sans-serif; margin:0;}/*! CSS Used from: Embedded */ .statsBlockContainer{display:inline-block;width:calc(33% - 10px);padding:5px;font-size:40px;font-weight:900;height:100px;vertical-align:top;} .statsBlockContainer div{font-size:15px;font-weight:300;} @media only screen and (max-width: 700px){ .statsBlockContainer{width:calc(50% - 10px);} } @media only screen and (max-width: 380px){ .statsBlockContainer{width:calc(100% - 10px);} }</style></head><body>";
            //Add each section.
            WriteSection(LibRpwsCore.lite_database.GetCollection<E_RPWS_User>("accounts").Count(), "Total Users", ref html);

            //Write
            Program.QuickWriteToDoc(e, html+"</html>");
        }

        private static void WriteSection(int count, string text, ref string html)
        {
            html += "<div class=\"statsBlockContainer\">"+count.ToString()+"<div>"+text+"</div></div>";
        }
    }
}
