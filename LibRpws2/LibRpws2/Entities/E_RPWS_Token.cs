using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpwsDatabase.Entities
{
    [Serializable]
    public class E_RPWS_Token
    {
        public string token { get; set; }
        public string accountUuid { get; set; }
        public long creationDate { get; set; }

        public bool isModern { get; set; } //If this is true, grant all permissions.
        public List<E_RPWS_Token_Permissions> permissions { get; set; } //Allowed permissions.

        public int _id { get; set; }

        public bool CheckIfAuthorized(E_RPWS_Token_Permissions requiredPermission)
        {
            //First, check to see if this is a legacy token. If it is, it will always be granted.
            if (isModern != true)
                return true;

            //Now, check to see if we have permission to access everything
            if (permissions.Contains(E_RPWS_Token_Permissions.All))
                return true;

            //Lastly, check to see if this token has access to the required permission.
            return permissions.Contains(requiredPermission);
        }
    }

    public enum E_RPWS_Token_Permissions
    {
        All, //All permissions
        Profile, //Account ID, email, name, installed apps (read only). Used for authentication.
        Locker, //Write access to the locker and liked apps
        Developer, //Access to developer features, such as publishing apps. Nothing to do with WebPebble
        TimelineReadOnly, //Read-only access to the timeline
        TimelineWrite, //Write access to the timeline

    }
}
