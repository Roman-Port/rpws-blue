using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpwsDatabase.Entities
{
    [Serializable]
    public class E_RPWS_TimelinePinContainer
    {
        public E_RPWS_TimelinePin pin { get; set; }
        public string userId { get; set; }
        public string urlId { get; set; }
        public string appId { get; set; }
        public string internalUuid { get; set; }
        public int internalAppVersion { get; set; }

        public int _id { get; set; }
    }
}
