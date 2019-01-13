using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpwsDatabase.Entities
{
    [Serializable]
    public class E_RPWS_TimelineSandboxToken
    {
        public string appUuid { get; set; }
        public string userUuid { get; set; }
        public string token { get; set; }
        public long creationDate { get; set; }

        public int _id { get; set; }
    }
}
