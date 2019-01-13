using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpws2.Entities
{
    public class SdkVersion
    {
        public int major { get; set; }
        public int minor { get; set; }
    }

    public class Application
    {
        public int timestamp { get; set; }
        public SdkVersion sdk_version { get; set; }
        public long crc { get; set; }
        public string name { get; set; }
        public int size { get; set; }
    }

    public class Resources
    {
        public int timestamp { get; set; }
        [Newtonsoft.Json.JsonIgnoreAttribute]
        public int crc { get; set; }
        public string name { get; set; }
        public int size { get; set; }
    }

    public class PebbleAppManifest
    {
        public int manifestVersion { get; set; }
        public string generatedBy { get; set; }
        public int generatedAt { get; set; }
        public Application application { get; set; }
        public string type { get; set; }
        
        public Resources resources { get; set; }
    }
}
