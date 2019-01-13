using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace LibRpwsDatabase.Entities
{
    [Serializable]
    public class E_RPWS_TimelinePin
    {
        [JsonIgnore]
        public int _id { get; set; } //Just for the database.

        //https://developer.get-rpws.com/guides/pebble-timeline/pin-structure/index.html

        public string id { get; set; } //The unique ID for this pin.
        public DateTime time { get; set; } //Start time. This isn't a long when sent over HTTP, so this is filled in later.
        public int duration { get; set; } //Time, in minutes, that this pin is active.
        public E_RPWS_TimelinePin_Notification createNotification { get; set; }
        public E_RPWS_TimelinePin_Notification updateNotification { get; set; }
        public E_RPWS_TimelinePin_Layout layout { get; set; }
        public E_RPWS_TimelinePin_Notification[] reminders { get; set; }
        public E_RPWS_TimelinePin_Action[] actions { get; set; }

        //Metadata sent
        public string[] topicKeys { get; set; }
        public string source { get; set; }
        public string dataSource { get; set; }//ex. "uuid:"
        public string guid { get; set; }
        public DateTime createTime { get; set; }
        public DateTime updateTime { get; set; }
    }

    [Serializable]
    public class E_RPWS_TimelinePin_Notification
    {
        public E_RPWS_TimelinePin_Layout layout { get; set; }
        public DateTime time { get; set; } //Another DateTime that is serialized into a long.
    }

    [Serializable]
    public class E_RPWS_TimelinePin_Layout
    {
        public string type { get; set; }
        public string title { get; set; }
        public string subtitle { get; set; }
        public string body { get; set; }
        public string tinyIcon { get; set; }
        public string smallIcon { get; set; }
        public string largeIcon { get; set; }
        
        public string primaryColor { get; set; }
        public string secondaryColor { get; set; }
        public string backgroundColor { get; set; }
        public string locationName { get; set; }
        public string[] headings { get; set; }
        public string[] paragraphs { get; set; }
        public DateTime lastUpdated { get; set; } //Another example of a DateTime that is serialized into a long.
    }

    [Serializable]
    public class E_RPWS_TimelinePin_Action
    {
        public string title { get; set; }
        public string type { get; set; }
        [JsonIgnoreAttribute]
        public int launchCode { get; set; } //legacy
        [JsonProperty(PropertyName = "launchCode")]
        public long launchCodeLong { get; set; }
    }
}
