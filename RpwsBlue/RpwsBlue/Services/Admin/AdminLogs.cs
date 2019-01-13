using LibRpws;
using LibRpwsDatabase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace RpwsBlue.Services.Admin
{
    class AdminLogs
    {
        public static string OnReq(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //Get all logs.
            E_RPWS_StatisticObject_Status status = Enum.Parse<E_RPWS_StatisticObject_Status>(e.Request.Query["type"]);
            var enteries = LibRpwsCore.lite_database.GetCollection<E_RPWS_StatisticObject>("stats").Find(x => x.status == status).ToArray();
            string o = "<h1>" + status.ToString() + "s</h1>";
            o += "<table width=\"100%\"><tr><th>User ID</th><th>Path</th><th>IP</th><th>Time</th></tr>";
            for(int i = 0; i< enteries.Length; i++)
            {
                var u = enteries[i];
                o += "<tr><td>";
                //var user = LibRpws.Users.LibRpwsUsers.GetUserByUuid(u.user_uuid, ee);
                //o += user.name;]
                o += u.user_uuid;
                o += "</td><td>";
                o += u.path;
                o += "</td><td>";
                o += new IPAddress(u.ip_addr).ToString();
                o += "</td><td>";
                if(u.message == null)
                    o += "<i>No Message</i>";
                else
                    o += "<a href=\"#\" onclick=\"WriteError(" + i.ToString() + ");\">Show</a>";
                o += "</td><td>";
                DateTime dt = new DateTime(u.time);
                o += dt.ToLongDateString() + " " + dt.ToShortTimeString();
                o += "</td></tr>";
            }
            o += "</table>";
            //Write data table.
            o += "<div style=\"display:none;\">";
            for(int i = 0; i<enteries.Length; i++)
            {
                var u = enteries[i];
                if(u.message != null)
                {
                    o += "<div id=\"c_" + i.ToString() + "\">";
                    o += System.Web.HttpUtility.HtmlEncode(u.message).Replace("\n", "<br>");
                    o += "</div>";
                }
            }
            o += "</div>";
            //Write the JS window.
            o += "<div id=\"custombg\" class=\"hidden\" style=\"position:fixed; top:0; left:0; bottom:0; right:0; background-color:#000000b8;\"><div id=\"customwindow\" style=\"position:fixed; top:25%; bottom:25%; left:25%; right:25%; z-index:10; background-color:white;\"></div></div>";
            //Insert JS
            o += "<script>function WriteError(id){var e = document.getElementById('c_'+id.toString()); document.getElementById('custombg').className = ''; document.getElementById('customwindow').innerHTML = '<a href=\"#\" onclick=\"HideDialog();\">Close</a><br><br>'+ e.innerHTML;} function HideDialog() {document.getElementById('custombg').className = 'hidden';}</script>";
            return o;

        }
    }
}
