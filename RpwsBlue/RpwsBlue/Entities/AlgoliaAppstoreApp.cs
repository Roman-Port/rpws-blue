using LibRpws2.Entities;
using RpwsBlue.Entities.Algolia;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Entities
{
    public class AlgoliaAppstoreApp
    {
        public string id { get; set; }
        public string companions { get; set; }
        public string title { get; set; }
        public string uuid { get; set; }
        public string author { get; set; }
        public string author_email { get; set; }
        public string[] tags { get; set; }
        public string category { get; set; }
        public string category_id { get; set; }
        public string[] collections { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public int hearts { get; set; }
        public int installs { get; set; }
        public PebbleApp_Compatibility compatibility { get; set; }
        public string original_title { get; set; }
        public List<AssetCollection> asset_collections { get; set; }
        public string list_image { get; set; }
        public string icon_image { get; set; }
        public List<string> screenshot_images { get; set; }
        public string version { get; set; }
        public string js_version { get; set; }
        public object js_versions { get; set; }
        public List<string> capabilities { get; set; }
        public List<string> _tags { get; set; }
        public string objectID { get; set; }

        /// <summary>
        /// Convert this to the Algolia application.
        /// </summary>
        /// <param name="a"></param>
        public AlgoliaAppstoreApp(AppstoreApp a, int hearts, int installs)
        {
            id = a.id;
            companions = "00";
            title = a.title;
            uuid = a.uuid;
            author = a.author;
            author_email = "";
            tags = new string[0];
            category = a.category_name;
            category_id = a.category_id;
            collections = new string[0];
            description = a.description;
            type = a.type;
            this.hearts = hearts;
            this.installs = installs;
            compatibility = a.compatibility;
            original_title = a.title;
            asset_collections = new List<AssetCollection>();
            foreach (string s in a.screenshot_images.Keys)
                asset_collections.Add(new AssetCollection(a, s));
            list_image = a.list_image.GetItem()._144x144;
            icon_image = a.icon_image.GetItem()._48x48;
            screenshot_images = new List<string>();
            foreach(var s in a.screenshot_images.GetItem())
            {
                if (s._144x168 == null)
                    screenshot_images.Add(s._180x180);
                else
                    screenshot_images.Add(s._144x168);
            }
            version = a.latest_release.version;
            js_version = a.latest_release.version;
            js_versions = null;
            capabilities = new List<string>();
            objectID = a.id;

            //Add tags
            _tags = new List<string>();
            if(a.table_meta.platform_tags == null)
            {
                //Add platform tags.
                List<string> s = new List<string>();
                if (a.compatibility.aplite.supported)
                    s.Add("aplite");
                if (a.compatibility.basalt.supported)
                    s.Add("basalt");
                if (a.compatibility.chalk.supported)
                    s.Add("chalk");
                if (a.compatibility.diorite.supported)
                    s.Add("diorite");
                if (a.compatibility.emery.supported)
                    s.Add("emery");
                a.table_meta.platform_tags = s.ToArray();
            }
            _tags.AddRange(a.table_meta.platform_tags);
            _tags.Add(a.type);
            _tags.Add("category_" + a.category_id);
            _tags.Add("developer_" + a.developer_id);
        }
    }
}

namespace RpwsBlue.Entities.Algolia
{ 
    public class AssetCollection
    {
        public string description { get; set; }
        public List<string> screenshots { get; set; }
        public string hardware_platform { get; set; }

        public AssetCollection(AppstoreApp a, string hardware_platform)
        {
            this.hardware_platform = hardware_platform;
            description = a.description;
            screenshots = new List<string>();
            foreach(var s in a.screenshot_images[hardware_platform])
            {
                if (s._144x168 == null)
                    screenshots.Add(s._180x180);
                else
                    screenshots.Add(s._144x168);
            }
        }
    }
}
