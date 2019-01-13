using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpwsDatabase.Entities
{
    [Serializable]
    public class E_RPWS_HeartObject
    {
        public int heartOffset { get; set; }
        public string appId { get; set; }

        public int _id { get; set; }
    }
}
