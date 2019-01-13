using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpwsDatabase.Entities
{
    [Serializable]
    public class E_RPWS_AppToken
    {
        public string token { get; set; }
        public string accountUuid { get; set; }
        public long creationDate { get; set; }
        public string appId { get; set; }
        public bool isOriginalPebble { get; set; }

        public int _id { get; set; }
    }
}
