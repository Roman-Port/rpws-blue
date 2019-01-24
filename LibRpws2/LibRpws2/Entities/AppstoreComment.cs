using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpws2.Entities
{
    public class AppstoreComment
    {
        public long time;

        public string user_id;
        public string user_uuid;
        public string user_name;
        public string user_profile_image;

        public int total_votes;
        public Dictionary<string, int> vote_users = new Dictionary<string, int>();

        public string content;
    }
}
