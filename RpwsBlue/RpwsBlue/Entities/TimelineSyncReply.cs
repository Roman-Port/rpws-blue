using LibRpwsDatabase.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Entities
{
    public class TimelineSyncReply
    {
        public string syncURL;
        public TimelineSyncReplyAction[] updates;
    }

    public class TimelineSyncReplyAction
    {
        public string type;
        public E_RPWS_TimelinePin data;
    }
}
