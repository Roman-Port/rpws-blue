using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpwsDatabase.Entities
{
    public class E_RPWS_AnalyticsObject
    {
        public int _id { get; set; }

        public long time { get; set; }

        public int serviceId { get; set; }
        public long timeTicks { get; set; }

        public bool ok { get; set; }
        public string error { get; set; }
    }
}
