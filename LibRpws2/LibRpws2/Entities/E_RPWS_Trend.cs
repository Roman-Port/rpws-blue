using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpwsDatabase.Entities
{
    [Serializable]
    public class E_RPWS_Trend
    {
        public string objectId { get; set; } //Like an app ID
        public long hour { get; set; } //Day, starting January 1st, 2018.
        public long day { get; set; } //Day, starting January 1st, 2018.
        public int hits { get; set; } //Number of users hitting this trend (number of times this app ID was clicked, ect)
        public int type { get; set; } //View enum below
        public string[] accountIds { get; set; } //List of account IDs that have contributed to this trend

        public int _id { get; set; }
    }

    public enum E_RPWS_TrendType
    {
        AppClickWatchfaces = 10,
        AppClickWatchapps = 11,
        AppInstall = 13,
        AppstoreSearch = 14
    }
}
