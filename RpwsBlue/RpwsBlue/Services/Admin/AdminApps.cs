using LibRpws;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LibRpws2.Entities;

namespace RpwsBlue.Services.Admin
{
    static class AdminApps
    {
        public static string OnReq(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //Get all apps that are not original
            var collection = LibRpwsCore.lite_database.GetCollection<PebbleAppDbStorage>("edited_apps");
            var editedApps = collection.Find(x => x.app.isOriginal == 0).ToArray();
            //Write a table with all of the apps
            string o = "";
            o += "<table width=\"100%\"><tr><th>Name</th><th>Type</th><th>ID</th><th>Author</th><th>Author ID</th><th>Published?</th><th>Links</th></tr>";
            foreach(var app in editedApps)
            {
                o += "<tr><td>";
                o += app.app.title;
                o += "</td><td>";
                o += app.app.type;
                o += "</td><td>";
                o += "<a href=\"//apps.get-rpws.com/" + app.app.id + "\" target=\"_blank\">";
                o += app.app.id;
                o += "</a>";
                o += "</td><td>";
                o += app.app.author;
                o += "</td><td>";
                o += app.app.developer_id;
                o += "</td><td>";
                o += (app.app.isPublished == 1).ToString();
                o += "</td><td>";
                o += "<a href=\"/admin/?service=apps_transferowner_dialog&id="+ app.app.id + "\">Transfer Ownership</a> ";
                o += "<a href=\"/publish/" + app.app.id + "/manage/\" target=\"_blank\">Manage</a>";
                o += "</td></tr>";
            }
            o += "</table>";
            //Add bottom buttons
            o += "<hr>";
            o += "Migrate existing app";
            o += "<form style=\"margin-left:10px;\" action=\"/admin/?service=app_migrate\" method=\"post\">App ID <input type=\"text\" name=\"app\"><br>Migrate to author ID <input type=\"text\" name=\"rec\" value=\""+ ee.user.pebbleId +"\"><br><input type=\"submit\">";
            return o;
        }
    }
}
