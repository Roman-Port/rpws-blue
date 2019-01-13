using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibRpws2.Entities
{
    
    public class PebbleApp_Capabilities
    {
        public int is_timeline_enabled { get; set; }
        public int is_configurable { get; set; }

        public static PebbleApp_Capabilities ConvertFromAppinfo(string[] d)
        {
            PebbleApp_Capabilities c = new PebbleApp_Capabilities();
            if(d == null)
            {
                c.is_timeline_enabled = 1;
                c.is_configurable = 1;
                return c;
            } else
            {
                c.is_configurable = 0;
                if (d.Contains("configurable"))
                    c.is_configurable = 1;
                c.is_timeline_enabled = 0;
                return c;
            }
        }
    }

    
    public class PebbleApp_RPWS_Dev
    {
        public PebbleApp_Release[] release_history { get; set; }
        public bool changes_pending { get; set; }

        public long last_edit_time { get; set; }

        //Db version >= 6
        public long last_release_time { get; set; }
        public bool is_published { get; set; }
        public int changes_since_last_published { get; set; }
        public Dictionary<string, List<long>> big_change_dates { get; set; } //Used to rate limit changes to things such as name or category. Key is type, list long is datetime.
    }

    
    public class AppstoreApp
    {

        public string author { get; set; }
        //Capabilities

        public string category_id { get; set; }

        public string category_name { get; set; }

        public string category_color { get; set; }

        public PebbleApp_ChangelogItem[] changelog { get; set; }

        public PebbleApp_Companions companions { get; set; }

        public PebbleApp_Compatibility compatibility { get; set; }

        public string created_at { get; set; }

        public string description { get; set; }

        public string developer_id { get; set; }

        public List<PebbleApp_HeaderImg> header_images { get; set; }

        [Obsolete("This value has been replaced. This will always display the legacy value from official Pebble servers. Do not use!")]
        public int hearts { get; set; }

        public string id { get; set; }

        public PebbleApp_Release latest_release { get; set; }

        public PebbleApp_Links links { get; set; }

        public Dictionary<string, PebbleApp_ListImg> list_image { get; set; }

        public string published_date { get; set; }

        public Dictionary<string, List<PebbleApp_ScreenshotImg>> screenshot_images { get; set; } //To convert

        public string source { get; set; }

        public string title { get; set; }

        public string type { get; set; }

        public string uuid { get; set; }

        public string website { get; set; }

        public Dictionary<string, PebbleApp_IconImg> icon_image { get; set; }

        /* New, just for table */

        public TableMeta table_meta { get; set; }

        /* Legacy. This isn't stored in the database and is only used for migrations */

        [Obsolete("This was used only for migrations.")]
        public PebbleApp_Capabilities capabilities;
        [Obsolete("This was used only for migrations.")]
        public AppMeta meta;
        [Obsolete("This was used only for migrations.")]
        public int isOriginal;
        [Obsolete("This was used only for migrations.")]
        public int isPublished;
        [Obsolete("This was used only for migrations.")]
        public int isTimelineKnownUnsupported;

    }

    /// <summary>
    /// Special dictonary that allows easier lookup.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class PebblePlatformDictExtensions
    {
        public static T GetItem<T>(this Dictionary<string, T> dict, bool throwErrorOnFail = true)
        {
            if(dict == null)
            {
                if (throwErrorOnFail)
                    throw new Exception("Null object.");
                else
                    return default(T);
            }

            string[] platforms = new string[] { "basalt", "emery", "diorite", "chalk", "aplite" };
            
            foreach(string s in platforms)
            {
                if (dict.ContainsKeyNotNull(s))
                    return dict[s];
            }

            //Invalid
            if (throwErrorOnFail)
                throw new Exception("No valid platform.");
            else
                return default(T);
        }

        public static T GetItem<T>(this Dictionary<string, T> dict, string preferredPlatform, bool throwErrorOnFail = true)
        {
            if (dict == null)
            {
                if (throwErrorOnFail)
                    throw new Exception("Null object.");
                else
                    return default(T);
            }

            string[] platforms = new string[] { "basalt", "emery", "diorite", "chalk", "aplite" };

            if (dict.ContainsKeyNotNull(preferredPlatform))
                return dict[preferredPlatform];

            foreach (string s in platforms)
            {
                if (dict.ContainsKeyNotNull(s))
                    return dict[s];
            }

            //Invalid
            if (throwErrorOnFail)
                throw new Exception("No valid platform.");
            else
                return default(T);
        }

        public static bool ContainsKeyNotNull<T>(this Dictionary<string, T> dict, string key)
        {
            if (!dict.ContainsKey(key))
                return false;
            return dict[key] != null;
        }

        public static void AddOrReplace<T>(this Dictionary<string, T> dict, T data, string key)
        {
            if (dict.ContainsKey(key))
            {
                dict.Remove(key);
            }
            dict.Add(key, data);
        }
    }

    /// <summary>
    /// Used on Blue in v3 to store apps.
    /// </summary>
    public class SimpleAppstoreAppDbStorage
    {
        public int _id { get; set; }
        public AppstoreApp app { get; set; }
        public int dbVersion { get; set; }

        public string id { get; set; }
        public string uuid { get; set; }
    }

    public class PebbleAppDbStorage
    {
        public int _id { get; set; }
        public AppstoreApp app { get; set; }
        public int dbVersion { get; set; }
        public PebbleApp_RPWS_Dev dev { get; set; }
        public bool deleted { get; set; }
    }

    public class TableMeta
    {
        public PebbleApp_Capabilities capabilities { get; set; }
        public bool isOriginal { get; set; }
        public bool isPublished { get; set; }
        public bool isTimelineKnownUnsupported { get; set; }
        public bool isDeleted { get; set; }

        public int table_version { get; set; }

        public string[] platform_tags { get; set; } //Tags: aplite, basalt, chalk, diorite, emery, ios, android

        [Obsolete("Used only for migrations. Do not use.")]
        public PbwAppInfo2 appinfo { get; set; }

        public PbwInfo pbw_info { get; set; }
    }

    

    public class PbwAppInfo2
    {
        public string[] targetPlatforms { get; set; }
        public string name { get; set; }
        public string companyName { get; set; }
        public string sdkVersion { get; set; }
        public string[] capabilities { get; set; }
        public string versionLabel { get; set; }
        public string longName { get; set; }
        public string displayName { get; set; }
        public string shortName { get; set; }
        public string uuid { get; set; }
    }

    public class AppMeta2
    {
        public string header { get; set; }
        public string struct_version { get; set; }
        public string sdk_version { get; set; }
        public string app_version { get; set; }
        public int size { get; set; }
        public int offset { get; set; }
        public int crc { get; set; }
        public string appname { get; set; }
        public string companyname { get; set; }
        public long icon_resource_id { get; set; }
        public long symbol_table_address { get; set; }
        public long pebble_process_info_flags { get; set; }
        public long relocation_list { get; set; }
        public string uuid { get; set; }
    }


    public class PebbleAppOutput_ScreenshotImages
    {

        public PebbleApp_ScreenshotImg[] aplite { get; set; }

        public PebbleApp_ScreenshotImg[] basalt { get; set; }

        public PebbleApp_ScreenshotImg[] chalk { get; set; }

        public PebbleApp_ScreenshotImg[] diorite { get; set; }

        public PebbleApp_ScreenshotImg[] emery { get; set; }

        public PebbleApp_ScreenshotImg[] GetScreenshotImages()
        {
            PebbleApp_ScreenshotImg[] release = basalt;
            if (!CheckIfValid(release))
                release = diorite;
            if (!CheckIfValid(release))
                release = emery;
            if (!CheckIfValid(release))
                release = chalk;
            if (!CheckIfValid(release))
                release = aplite;
            if (!CheckIfValid(release))
                throw new Exception("No screenshot images found.");
            return release;
        }

        private bool CheckIfValid(PebbleApp_ScreenshotImg[] r)
        {
            if (r == null)
                return false;
            if (r.Length == 0)
                return false;
            return true;
        }
    }

    
    public class PebbleAppOutput_Releases
    {

        public PebbleApp_Release aplite { get; set; }

        public PebbleApp_Release basalt { get; set; }

        public PebbleApp_Release chalk { get; set; }

        public PebbleApp_Release diorite { get; set; }

        public PebbleApp_Release emery { get; set; }

        public PebbleApp_Release GetRelease()
        {
            PebbleApp_Release release = basalt;
            if (release == null)
                release = diorite;
            if (release == null)
                release = emery;
            if (release == null)
                release = chalk;
            if (release == null)
                release = aplite;
            if (release == null)
                throw new Exception("No app releases found.");
            return release;
        }
    }

    
    public class PebbleAppOutput_IconImages
    {

        public PebbleApp_IconImg aplite { get; set; }

        public PebbleApp_IconImg basalt { get; set; }

        public PebbleApp_IconImg chalk { get; set; }

        public PebbleApp_IconImg diorite { get; set; }

        public PebbleApp_IconImg emery { get; set; }

        public PebbleApp_IconImg GetIcon()
        {
            PebbleApp_IconImg release = basalt;
            if (release == null)
                release = diorite;
            if (release == null)
                release = emery;
            if (release == null)
                release = chalk;
            if (release == null)
                release = aplite;
            if (release == null)
                throw new Exception("No app icons found.");
            return release;
        }
    }

    
    public class PebbleAppOutput_ListImages
    {

        public PebbleApp_ListImg aplite { get; set; }

        public PebbleApp_ListImg basalt { get; set; }

        public PebbleApp_ListImg chalk { get; set; }

        public PebbleApp_ListImg diorite { get; set; }

        public PebbleApp_ListImg emery { get; set; }

        public PebbleApp_ListImg GetListImage()
        {
            PebbleApp_ListImg release = basalt;
            if (release == null)
                release = diorite;
            if (release == null)
                release = emery;
            if (release == null)
                release = chalk;
            if (release == null)
                release = aplite;
            if (release == null)
                throw new Exception("No list images found.");
            return release;
        }
    }


    public class PebbleApp_ChangelogItem
    {

        public string version { get; set; }

        public string published_date { get; set; }

        public string release_notes { get; set; }
    }

    
    public class PebbleApp_Companions
    {

        public CompanionPlatform ios { get; set; }

        public CompanionPlatform android { get; set; }

    }

    
    public class CompanionPlatform
    {
        public string id { get; set; }
        public string icon { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public bool required { get; set; }
        public string pebblekit_version { get; set; }
    }

    
    public class PebbleApp_Compatibility
    {

        public PebbleApp_Compatibility_Phone ios { get; set; }

        public PebbleApp_Compatibility_Phone android { get; set; }

        public PebbleApp_Compatibility_Hardware emery { get; set; }

        public PebbleApp_Compatibility_Hardware diorite { get; set; }

        public PebbleApp_Compatibility_Hardware chalk { get; set; }

        public PebbleApp_Compatibility_Hardware basalt { get; set; }

        public PebbleApp_Compatibility_Hardware aplite { get; set; }

        public PebbleApp_Compatibility_Hardware GetHardwareByName(string name)
        {
            switch(name)
            {
                case "aplite": return aplite;
                case "basalt": return basalt;
                case "chalk": return chalk;
                case "diorite": return diorite;
                case "emery": return emery;
                default: throw new Exception("Invalid platform!");
            }
        }
    }

    
    public class PebbleApp_Compatibility_Hardware
    {

        public PebbleApp_Compatibility_Hardware_Firmware firmware { get; set; }

        public bool supported { get; set; }
    }

    
    public class PebbleApp_Compatibility_Hardware_Firmware
    {

        public int major { get; set; }
    }

    
    public class PebbleApp_Compatibility_Phone
    {

        public bool supported { get; set; }
    }

    
    public class PebbleApp_HeaderImg
    {

        public string orig { get; set; }
    }

    
    public class PebbleApp_IconImg
    {
        [JsonProperty("48x48")]
        public string _48x48 { get; set; }
    }

    
    public class PebbleApp_ListImg
    {
        [JsonProperty("144x144")]
        public string _144x144 { get; set; }
    }

    
    public class PebbleApp_ScreenshotImg
    {
        [JsonProperty("144x168")]
        public string _144x168 { get; set; }
        [JsonProperty("180x180")]
        public string _180x180 { get; set; }
    }

    
    public class PebbleApp_Release
    {

        public string id { get; set; }

        public float js_version { get; set; }

        public string pbw_file { get; set; }

        public string published_date { get; set; }

        public string release_notes { get; set; }

        public string version { get; set; }
    }

    
    public class PebbleApp_Links
    {

        public string add { get; set; }

        public string remove { get; set; }

        public string add_heart { get; set; }

        public string remove_heart { get; set; }

        public string add_flag { get; set; }

        public string remove_flag { get; set; }

        public string share { get; set; }
    }

    
    public enum WatchHardware
    {
        aplite, //OG Pebble / Pebble Steel
        basalt, //Pebble Time / Pebble Time Steel
        chalk, //Pebble Time Round
        diorite, //Pebble 2
        emery //Pebble Time 2
    }

    
    public class AppMeta
    {
        public string header { get; set; }
        public string struct_version { get; set; }
        public string sdk_version { get; set; }
        public string app_version { get; set; }
        public int size { get; set; }
        public int offset { get; set; }
        public int crc { get; set; }
        public string appname { get; set; }
        public string companyname { get; set; }
        public long icon_resource_id { get; set; }
        public long symbol_table_address { get; set; }
        public long pebble_process_info_flags { get; set; }
        public long relocation_list { get; set; }
        public string uuid { get; set; }

        //Even newer
        public bool isTimelineEnabled { get; set; }
    }
}
