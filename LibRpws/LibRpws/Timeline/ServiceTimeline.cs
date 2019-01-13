using LibRpwsDatabase.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpws.Timeline
{
    public static class ServiceTimeline
    {
        public static void AddEvent(E_RPWS_TimelineEventAction action, string pinId, HttpSession session, E_RPWS_User user)
        {
            E_RPWS_TimelineEvent ev = new E_RPWS_TimelineEvent();
            ev.action = action;
            ev.relatedPinId = pinId;
            ev.time = DateTime.UtcNow;
            ev.userUuid = user.uuid;
            LibRpwsCore.lite_database.GetCollection<E_RPWS_TimelineEvent>("timeline_events").Insert(ev);
        }

        public static LiteDB.LiteCollection<E_RPWS_TimelineSandboxToken> GetSandboxTokenCollection()
        {
            return LibRpwsCore.lite_database.GetCollection<E_RPWS_TimelineSandboxToken>("timeline_sandbox_tokens");
        }
    }
}
