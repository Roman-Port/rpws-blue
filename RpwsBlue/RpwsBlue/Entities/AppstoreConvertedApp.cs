using LibRpws2.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Entities
{
    public class AppstoreConvertedApp
    {
        public string author { get; set; }
        public string category_id { get; set; }
        public string category_name { get; set; }
        public string category_color { get; set; }
        public PebbleApp_ChangelogItem[] changelog { get; set; }
        public PebbleApp_Companions companions { get; set; }
        public string created_at { get; set; }
        public string description { get; set; }
        public string developer_id { get; set; }
        public PebbleApp_HeaderImg[] header_images { get; set; }
        public int hearts { get; set; }
        public string id { get; set; }
        public PebbleApp_IconImg icon_image { get; set; }
        public PebbleApp_Release latest_release { get; set; }
        public PebbleApp_Links links { get; set; }
        public PebbleApp_ListImg list_image { get; set; }
        public string published_date { get; set; }
        public PebbleApp_ScreenshotImg[] screenshot_images { get; set; }
        public string source { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public string uuid { get; set; }
        public string website { get; set; }

        /// <summary>
        /// Convert this to an Appstore app.
        /// </summary>
        /// <param name="a"></param>
        public AppstoreConvertedApp(AppstoreApp a, string platform)
        {
            Convert(a, platform);
        }

        public void Convert(AppstoreApp a, string platform)
        {
            author = ProcessSafeString(a.author);
            category_id = a.category_id;
            category_name = a.category_name;
            category_color = a.category_color;
            changelog = a.changelog;
            companions = a.companions;
            created_at = a.created_at;
            description = ProcessSafeString(a.description);
            developer_id = a.developer_id;
            if (a.header_images != null)
                header_images = a.header_images.ToArray();
            hearts = AppstoreVoteApi.GetAppTotalVotes(a.id);
            id = a.id;
            icon_image = a.icon_image.GetItem(platform, false);
            latest_release = a.latest_release;
            links = GenerateLinks(id);
            list_image = a.list_image.GetItem(platform);
            published_date = a.published_date;
            screenshot_images = a.screenshot_images.GetItem(platform).ToArray();
            source = a.source;
            title = ProcessSafeString(a.title);
            type = a.type;
            uuid = a.uuid;
            website = a.website;
        }

        public static string ProcessSafeString(string s)
        {
            //Protect against injection and clean it up.
            return System.Web.HttpUtility.HtmlEncode(s).Replace("\n", "<br>");
        }

        private static PebbleApp_Links GenerateLinks(string id)
        {
            return new PebbleApp_Links
            {
                add = $"https://{Program.config.public_host}/v1/locker/{id}/",
                add_flag = $"https://{Program.config.public_host}/v2/appstore/app/{id}/add_flag/",
                add_heart = $"https://{Program.config.public_host}/v2/appstore/app/{id}/upvote/",
                remove = $"https://{Program.config.public_host}/v1/locker/{id}/",
                remove_flag = $"https://{Program.config.public_host}/v2/appstore/app/{id}/remove_flag/",
                remove_heart = $"https://{Program.config.public_host}/v2/appstore/app/{id}/downvote/",
                share = $"https://app.get-rpws.com/{id}"
            };
        }
    }
}
