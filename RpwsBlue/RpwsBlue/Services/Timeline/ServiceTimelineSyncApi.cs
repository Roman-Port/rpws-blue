using LibRpws;
using LibRpwsDatabase.Entities;
using Newtonsoft.Json;
using RpwsBlue.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace RpwsBlue.Services.Timeline
{
    public static class ServiceTimelineSyncApi
    {
        public static void OnSyncRequest(Microsoft.AspNetCore.Http.HttpContext context, HttpSession ee)
        {
            //Generate a reply.
            TimelineSyncReply r = new TimelineSyncReply();
            List<TimelineSyncReplyAction> actions = new List<TimelineSyncReplyAction>();
            //Get the time requested
            DateTime t = new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            if(ee.GET.ContainsKey("t"))
            {
                try
                {
                    t = new DateTime(long.Parse(ee.GET["t"]));
                } catch
                {
                    //invalid time
                    throw new Exception("The time you offered with ?t= is invalid. Try removing it.");
                }
            }
            //Get all actions since this date.
            var actionsCollection = LibRpwsCore.lite_database.GetCollection<E_RPWS_TimelineEvent>("timeline_events");
            var pinsCollection = LibRpwsCore.lite_database.GetCollection<E_RPWS_TimelinePinContainer>("timeline_pins");
            var updates = actionsCollection.Find(x => x.userUuid == ee.user.uuid && x.time > t);
            //Todo: Ensure it's by date
            //Add each of these pins
            foreach(var a in updates)
            {
                TimelineSyncReplyAction aa = new TimelineSyncReplyAction();
                aa.type = a.action.ToString().Replace('_', '.');
                //Get the pin in question.
                var pins = pinsCollection.Find(x => x.internalUuid == a.relatedPinId && x.userId == a.userUuid && x.internalAppVersion >= 2).ToArray();
                if(pins.Length == 1)
                {
                    var pin = pins[0];
                    if (pin.pin.guid == null || pin.pin.createTime == null)
                        continue;
                    if (pin.pin.createTime.Year == 1)
                        continue;
                    //This is valid. Add the pin and add to the array.
                    aa.data = pin.pin;
                    actions.Add(aa);
                }
            }
            //Respond.
            r.updates = actions.ToArray();
            r.syncURL = "https://"+LibRpwsCore.config.public_host+"/v1/user/timeline/sync/?t="+DateTime.UtcNow.Ticks.ToString();
            JsonSerializerSettings sett = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            string outp = JsonConvert.SerializeObject(r, Formatting.None, sett);
            //Janky fix
            outp = outp.Replace(",\"lastUpdated\":\"0001-01-01T00:00:00\"", "");
            Program.QuickWriteToDoc(context, outp, "application/json", 200);
        }
    }
}
