using LibRpwsDatabase.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace LibRpws.Trends
{
    public static class TrendApi
    {
        public static LiteDB.LiteCollection<E_RPWS_Trend> GetCollection()
        {
            var collection = LibRpwsCore.lite_database.GetCollection<E_RPWS_Trend>("trends");
            return collection;
        }

        public static long GetHour(DateTime time)
        {
            //Get timestamp
            return (long)(time - new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalHours;
        }

        public static long GetDay(DateTime time)
        {
            //Get timestamp
            return (long)(time - new DateTime(2018, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalDays;
        }

        public static long GetHourNow(int offsetHour)
        {
            offsetHour = Math.Abs(offsetHour);
            return GetHour(DateTime.UtcNow) - offsetHour;
        }

        public static long GetDayNow(int dayOffset)
        {
            dayOffset = Math.Abs(dayOffset);
            return GetDay(DateTime.UtcNow) - dayOffset;
        }

        public static E_RPWS_Trend GetTrend(HttpSession session, E_RPWS_TrendType type, string objectId, int hourOffset)
        {
            int intType = (int)type;
            long hour = GetHourNow(hourOffset);
            E_RPWS_Trend[] trends = GetCollection().Find(x => x.type == intType && x.objectId == objectId && x.hour == hour).ToArray();
            if(trends.Length == 0)
            {
                //Add it and return a new one.
                E_RPWS_Trend trend = new E_RPWS_Trend();
                trend.accountIds = new string[0];
                trend.hits = 0;
                trend.day = GetDayNow(hourOffset / 24);
                trend.hour = GetHourNow(hourOffset);
                trend.objectId = objectId;
                trend.type = (int)type;
                //Insert it.
                trend._id = GetCollection().Insert(trend);
                return trend;
            }
            return trends[0];
        }

        public static void CountTrend(HttpSession session, E_RPWS_TrendType type, string objectId, string accountId)
        {
            //Get the current trend object.
            E_RPWS_Trend trend = GetTrend(session, type, objectId, 0);
            //Check if our account ID exists in the trend.
            if(!trend.accountIds.Contains(accountId))
            {
                //We need to register this.
                var accountIds = trend.accountIds.ToList();
                accountIds.Add(accountId);
                trend.accountIds = accountIds.ToArray();
                //Add hit
                trend.hits++;
                //Update.
                GetCollection().Update(trend);
            }
        }

        public static string[] GetRecentTrendObjects(HttpSession session, E_RPWS_TrendType type)
        {
            //Get the last 5 days of data.
            long currentDay = GetDayNow(0);
            //Get data.
            int trendInt = (int)type;
            E_RPWS_Trend[] trends = GetCollection().Find(x => x.type == trendInt && (x.day == currentDay || x.day == currentDay-1 || x.day == currentDay-2 || x.day == currentDay-3 || x.day == currentDay-4)).ToArray();
            //Apply multipliers to these based on how old they are.
            Dictionary<string, float> ranked = new Dictionary<string, float>();
            int max = 0;
            foreach(E_RPWS_Trend trend in trends)
            {
                //Calculate the multiplier.
                float multiplier = 6 - (currentDay - trend.day);
                //Check to see if this already has a value.
                float value = 0;
                if (!ranked.ContainsKey(trend.objectId))
                    ranked.Add(trend.objectId, 0);
                else
                    value = ranked[trend.objectId];
                //Add to the ranking
                value += multiplier * trend.hits;
                //Update
                ranked[trend.objectId] = value;
                max = (int)Math.Max(max, value+1);
            }
            //Now that all is sorted, add them in order. This is REALLY gross and probably the worst thing in this entire program.
            string[] sorted = new string[max];
            foreach(var value in ranked)
            {
                if (sorted[(int)value.Value] == null)
                {
                    //Set it normally.
                    sorted[(int)value.Value] = value.Key;
                } else
                {
                    //Yuck. Insert a spot...
                    List<string> sortedList = sorted.ToList();
                    sortedList.Insert((int)value.Value, value.Key);
                    sorted = sortedList.ToArray();
                }
            }
            string[] compressedSorted = new string[ranked.Count];
            int index = 0;
            for(int i = 0; i<sorted.Length; i++)
            {
                if (sorted[i] != null)
                {
                    compressedSorted[index] = sorted[i];
                    index++;
                }
            }
            //Yuck.
            Array.Reverse(compressedSorted);
            return compressedSorted;
        }
    }
}
