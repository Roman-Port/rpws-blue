using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpwsDatabase.Entities
{
    public class E_RPWS_StatisticObject
    {
        public int _id { get; set; }

        public string user_uuid { get; set; }
        public long time { get; set; }
        public byte[] ip_addr { get; set; }
        public E_RPWS_StatisticObject_Status status { get; set; }

        public string path { get; set; }
        public string message { get; set; }
    }

    public enum E_RPWS_StatisticObject_Status
    {
        Request,
        Message,
        Error,
        UserError
    }
}
