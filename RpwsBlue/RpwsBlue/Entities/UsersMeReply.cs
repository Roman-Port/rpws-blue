using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Entities
{
    public class UsersMeReply
    {
        public string[] applications;
        public UsersMeReplyUser[] users;
    }

    public class UsersMeReplyUser
    {
        public string id;
        public string uid;
        public string name;
        public string href;
        public string[] added_ids;
        public string[] voted_ids;
        public string[] flagged_ids;
        public string[] applications;
    }
}
