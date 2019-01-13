using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpwsDatabase.Entities
{
    [Serializable]
    public class E_RPWS_User
    {
        //User info
        public string email { get; set; }
        public string googleId { get; set; }
        public long registrationDate { get; set; }
        public string uuid { get; set; } //Used to identify ourselves internally.
        public string legacyPebbleId { get; set; } //OLD Pebble ID. 
        public bool isPebbleLinked { get; set; }
        public string pebbleId { get; set; }
        public bool isAppDev { get; set; }
        public string appDevName { get; set; }
        public string name { get; set; }

        //Apps
        public string[] lockerInstalled { get; set; } //IDs of installed apps.
        public string[] likedApps { get; set; }

        //Database stuff
        public int _id { get; set; }
    }
}
