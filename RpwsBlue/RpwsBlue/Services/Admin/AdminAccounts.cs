using LibRpws;
using LibRpwsDatabase.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace RpwsBlue.Services.Admin
{
    class AdminAccounts
    {
        public static string OnReq(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //Get all apps that are not original
            var collection = LibRpwsCore.lite_database.GetCollection<E_RPWS_User>("accounts");
            //List all users
            var users = collection.Find(x => true).ToArray();
            //List all
            string o = "";
            o += "<table width=\"100%\"><tr><th>Name</th><th>Status</th><th>Email</th><th>Pebble ID</th><th>Register Date</th></tr>";
            foreach (var u in users)
            {
                o += "<tr><td>";
                o += u.name;
                o += "</td><td>";
                string status = "Legacy (not migrated)";
                if (u.registrationDate == 0 && u.name != "Awesome User")
                    status = "Legacy (migrated)";
                if (u.registrationDate != 0)
                    status = "Blue+";
                o += status;
                o += "</td><td>";
                o += u.email;
                o += "</td><td>";
                o += u.pebbleId;
                o += "</td><td>";
                DateTime dt = new DateTime(u.registrationDate);
                o += dt.ToShortDateString() + " " + dt.ToLongTimeString();
                o += "</td></tr>";
            }
            o += "</table>";
            return o;
        }
    }
}
