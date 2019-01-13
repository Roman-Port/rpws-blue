using LibRpws;
using LibRpws2.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RpwsBlue.Services.PublishApi.PublishServices
{
    /// <summary>
    /// Used for changing listings.
    /// </summary>
    public static class ListingChange
    {
        public const int BAN_DAYS = 30; //Time before changes aren't checked anymore
        public const int BAN_MAX = 3; //Number of times a field can be changed in the time above.

        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee, PebbleAppDbStorage app)
        {
            //Decode payload.
            ListingChangePayload payload = Program.GetPostBodyJson<ListingChangePayload>(e);

            //Do action
            Program.QuickWriteJsonToDoc(e, MakeChange(app, payload));
        }

        static ListingChangeReply MakeChange(PebbleAppDbStorage app, ListingChangePayload payload)
        {
            //If this is a "big change", check and add to ratelimit.
            string ratelimitNote = null;
            if (payload.type == ListingChangeType.Title)
            {
                if (app.dev.big_change_dates == null)
                    app.dev.big_change_dates = new Dictionary<string, List<long>>();

                //Get entry for this, if it exists.
                string key = payload.type.ToString().ToLower();
                if (!app.dev.big_change_dates.ContainsKey(key))
                    app.dev.big_change_dates.Add(key, new List<long>());
                List<long> entry = app.dev.big_change_dates[key];

                //Find all enteries in the last 30 days.
                long sessionStart = DateTime.UtcNow.AddDays(-BAN_DAYS).Ticks;
                long[] daysInSession = entry.Where(x => x > sessionStart).ToArray();
                TimeSpan timeRemaining = new TimeSpan();

                //Find when the first ban expires if it will
                if(daysInSession.Length > 0)
                {
                    DateTime first = new DateTime(daysInSession[0]);
                    TimeSpan timeSince = DateTime.UtcNow - first;
                    timeRemaining = new TimeSpan(BAN_DAYS, 0, 0, 0) - timeSince;
                }

                //Check if we have exceeded the number of times we can change this field
                if (daysInSession.Length >= BAN_MAX)
                {
                    //Too many. Do not allow the change. Find when the first ban expires
                    return new ListingChangeReply
                    {
                        ok = false,
                        message = $"You have already changed this field {BAN_MAX} times in the last {BAN_DAYS} days. You may change it again in {timeRemaining.Days} days and {timeRemaining.Hours} hours."
                    };
                }

                //Add to the number of changes.
                app.dev.big_change_dates[key].Add(DateTime.UtcNow.Ticks);

                //Set note
                int numberOfRemainingChanges = BAN_MAX - daysInSession.Length - 1;
                DateTime expireDate;
                if (daysInSession.Length == 0)
                {
                    expireDate = DateTime.UtcNow.AddDays(BAN_DAYS);
                } else
                {
                    //Existing changes. Get first.
                    expireDate = DateTime.UtcNow.Add(timeRemaining);
                }
                string expireDateString = $"{expireDate.ToShortDateString()}";

                //Set the string
                if (numberOfRemainingChanges == 0)
                    ratelimitNote = $"Title changed. You may not change the {payload.type.ToString().ToLower()} again before {expireDateString}.";
                else if (numberOfRemainingChanges == 1)
                    ratelimitNote = $"You may change the {payload.type.ToString().ToLower()} of your app 1 more time before {expireDateString}.";
                else
                    ratelimitNote = $"You may change the {payload.type.ToString().ToLower()} of your app {numberOfRemainingChanges} more times before {expireDateString}.";
            }

            //Make change based on request.
            switch (payload.type)
            {
                case ListingChangeType.Title:
                    //Verify.
                    if(payload.value.Length < 4 || payload.value.Length > 20)
                    {
                        return new ListingChangeReply
                        {
                            message = "The title must be between 4-20 characters.",
                            ok = true
                        };
                    }
                    app.app.title = payload.value;
                    break;
                case ListingChangeType.Description:
                    app.app.description = payload.value;
                    break;
                case ListingChangeType.Source:
                    app.app.source = payload.value;
                    break;
                case ListingChangeType.Website:
                    app.app.website = payload.value;
                    break;
            }

            //Save the changes
            CorePublishApi.SaveApp(app);

            //Return OK
            return new ListingChangeReply
            {
                ok = true,
                message = ratelimitNote
            };
        }
    }

    class ListingChangePayload
    {
        public ListingChangeType type;
        public string value;
    }

    class ListingChangeReply
    {
        public bool ok;
        public string message;
    }

    enum ListingChangeType
    {
        Title, //Big change
        Description,
        Source,
        Website
    }
}
