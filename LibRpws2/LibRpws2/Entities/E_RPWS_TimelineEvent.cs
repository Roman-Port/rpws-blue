using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpwsDatabase.Entities
{
    [Serializable]
    public class E_RPWS_TimelineEvent
    {
        public int _id { get; set; }

        public E_RPWS_TimelineEventAction action { get; set; }
        public string userUuid { get; set; }
        public DateTime time { get; set; }
        public string relatedPinId { get; set; }
    }

    [Serializable]
    public enum E_RPWS_TimelineEventAction
    {
        timeline_pin_create,
        timeline_pin_update,
        timeline_pin_delete
    }
}
