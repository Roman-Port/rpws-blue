using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpws2.Entities
{
    public class E_LockerTokenV2
    {
        public bool installed { get; set; }
        public string token { get; set; }
        public string user_uuid { get; set; }

        public string app_uuid { get; set; }
        public string app_id { get; set; }

        public long creation_date { get; set; }
        public bool is_original_pebble { get; set; }

        public int _id { get; set; }
    }
}
