using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Entities
{
    public class Version
    {
        public string version { get; set; }
        public string release_notes { get; set; }
        public string release_date { get; set; }
    }

    public class Current
    {
        public int data { get; set; }
    }

    public class Size
    {
        public Current current { get; set; }
    }

    public class Content
    {
        public List<Version> versions { get; set; }
        public string icon { get; set; }
        public List<string> genres { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string price { get; set; }
        public List<string> screenshots { get; set; }
        public List<string> permissions { get; set; }
        public List<object> videos { get; set; }
        public string short_description { get; set; }
        public Size size { get; set; }
        public string feature_graphic { get; set; }
        public string content_rating { get; set; }
        public string slug { get; set; }
    }

    public class Params
    {
        public string country { get; set; }
        public string language { get; set; }
        public string id { get; set; }
        public string format { get; set; }
    }

    public class Request
    {
        public string path { get; set; }
        public string store { get; set; }
        public Params @params { get; set; }
        public string performed_at { get; set; }
    }

    public class Content2
    {
    }

    public class Metadata
    {
        public Request request { get; set; }
        public Content2 content { get; set; }
    }

    public class ApptweakReply
    {
        public Content content { get; set; }
        public Metadata metadata { get; set; }
    }
}
