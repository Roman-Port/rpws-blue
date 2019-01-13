using Algolia.Search;
using LibRpws;
using LibRpws2.Analytics;
using LibRpws2.ApiEntities;
using LibRpws2.Entities;
using RpwsBlue.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RpwsBlue.Services.Appstore
{
    /// <summary>
    /// Algolia searches!
    /// </summary>
    public class AppstoreSearch
    {
        static CacheBlock<Endpoint_SearchPage> cached_pages = new CacheBlock<Endpoint_SearchPage>(60, 60*10);

        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //Add XMLHTTP headers
            AppstoreSecureHeaders.SetSecureHeaders(e);

            Program.QuickWriteJsonToDoc(e, GeneratePage(e));
        }

        private static Endpoint_SearchPage GeneratePage(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Try to get from cache
            List<AppstoreListUiMessage> messages = new List<AppstoreListUiMessage>();
            string cache_key = e.Request.QueryString.ToString();
            if (cached_pages.TryGetItem(cache_key, out Endpoint_SearchPage page))
            {
                return page;
            }

            //First, generate an appstore query.
            var a = new RpwsAnalytics("Algolia search", "Create query");
            Query query = ParseSearchQuery(e.Request.Query, out int limit, out int offset, out int page_offset, out List<AppstoreListUiMessage> new_messages);
            messages.AddRange(new_messages);
            //Get hardware
            string hardware = "basalt";
            if (e.Request.Query.ContainsKey("hardware"))
                hardware = e.Request.Query["hardware"];
            //Run the query to Algolia.
            a.NextCheckpoint("Running query");
            var algoliaReply = GetAlgoliaReplyAsync(query).GetAwaiter().GetResult();
            a.NextCheckpoint("Getting full apps from database.");
            var results = algoliaReply["hits"];
            //Loop through the results and query their full data from the database.
            var apps = new List<AppstoreConvertedApp>();
            foreach (var o in results)
            {
                var original = AppstoreApi.GetAppById(o.Value<string>("id"));
                var p = new AppstoreConvertedApp(original, hardware);
                if (p != null)
                    apps.Add(p);
            }
            if (apps.Count == 0)
                messages.Add(new AppstoreListUiMessage("No results"));
            //Create the HTML from templates
            a.NextCheckpoint("Converting to HTML templates...");
            string apps_html = "";
            foreach(var m in messages)
            {
                apps_html += m.html;
            }
            foreach (var aa in apps)
            {
                apps_html += GetTemplate(aa);
            }
            a.NextCheckpoint("Creating output");
            //Now, create the standard reply. We'll have to respect templates.
            int totalCount = algoliaReply.Value<int>("nbHits");
            Endpoint_SearchPage output = new Endpoint_SearchPage
            {
                length = limit,
                total = totalCount,
                totalPages = algoliaReply.Value<int>("nbPages"),
                can = new Endpoint_SearchPageCan
                {
                    back = offset > 0,
                    next = (offset + limit) < totalCount
                },
                data = apps,
                maintenance = false,
                maintenance_msg = "",
                html = apps_html
            };
            a.End();
            a.DumpToConsole();

            //Insert in cache
            cached_pages.AddItem(output, cache_key);

            return output;
        }

        private static Dictionary<string, string> templateCache = new Dictionary<string, string>();

        /// <summary>
        /// Opens the template HTML file from the disk or from cache.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetTemplateBase(string type)
        {
            //Check if this template is in the cache.
            if (templateCache.ContainsKey(type))
                return templateCache[type];

            //We'll need to load it from the disk.
            string t = "";
            switch(type)
            {
                case "watchapp":
                    t = File.ReadAllText(Program.config.contentDir + "Services/Appstore/HtmlTemplates/watchapp.html");
                    templateCache.Add(type, t);
                    break;
                case "watchface":
                    t = File.ReadAllText(Program.config.contentDir + "Services/Appstore/HtmlTemplates/watchface.html");
                    templateCache.Add(type, t);
                    break;
            }

            return t;
        }

        public static string GetTemplate(AppstoreConvertedApp app)
        {
            //Get the template html.
            string html = GetTemplateBase(app.type);
            //Find the best screenshot image. This is legacy and should be changed later.
            PebbleApp_ScreenshotImg[] screenshots = app.screenshot_images;
            string screenshot = screenshots[0]._144x168;
            if (screenshot == null)
                screenshot = screenshots[0]._180x180;
            if (screenshot == null)
                screenshot = "https://romanport.com/static/Pebble/watchface_placeholder_icon.png";
            //Find the best icon image. This is legacy and should be changed later.
            string icon = "https://romanport.com/static/Pebble/watchface_placeholder_icon.png";
            if (app.icon_image != null)
            {
                icon = app.icon_image._48x48;
            }
            //Replace html.
            html = html.Replace("%%SCREENSHOT%%", screenshot);
            html = html.Replace("%%ICON%%", icon);
            html = html.Replace("%%NAME%%", app.title);
            html = html.Replace("%%CATEGORY%%", app.category_name);
            html = html.Replace("%%DESCRIPTION%%", app.description);
            html = html.Replace("%%RAND%%", System.Net.WebUtility.HtmlEncode(app.id + DateTime.UtcNow.Ticks.ToString()));
            html = html.Replace("%%ID%%", app.id);
            html = html.Replace("%%AUTHOR%%", app.author);
            return html;
        }

        public static Query ParseSearchQuery(Microsoft.AspNetCore.Http.IQueryCollection q, out int limit, out int offset, out int page_offset, out List<AppstoreListUiMessage> outMessages)
        {
            //Create the Algolia query from the search parameters.
            outMessages = new List<AppstoreListUiMessage>();
            List<string> tags = new List<string>();
            string searchQuery = "";
            //First, check the string query.
            if (q.ContainsKey("query"))
            {
                searchQuery = q["query"];
            }
            //Now, find required keys, such as the type, limit, and offset.
            if (!q.ContainsKey("type") || !q.ContainsKey("limit") || !q.ContainsKey("offset"))
                throw new RpwsStandardHttpException("Missing Required Key", "Missing one or more of the required queries in the query string, 'type', 'limit', or 'offset'.");
            //Add the type to the tags.
            tags.Add(q["type"]);
            //Parse limit and offset.
            if (!int.TryParse(q["limit"], out limit))
                //Failed
                throw new RpwsStandardHttpException("Invalid Integer", $"The 'limit' arg was not an integer. Instead, it was '{q["limit"]}.");
            if (!int.TryParse(q["offset"], out offset))
                //Failed
                throw new RpwsStandardHttpException("Invalid Integer", $"The 'offset' arg was not an integer. Instead, it was '{q["offset"]}.");
            //If the hardware is specified, add it as a tag. This would be aplite or basalt.
            if (q.ContainsKey("hardware") && q.ContainsKey("filterHardware"))
            {
                if (q["filterHardware"] == "true")
                    tags.Add(q["hardware"]);
            }
            //Do the same with the phone.
            if (q.ContainsKey("phone"))
                tags.Add(q["phone"]);
            //Do the same with the category.
            if (q.ContainsKey("category"))
                if (q["category"].ToString().Length > 0)
                    tags.Add("category_" + q["category"]);
            //If requested to filter hardware
            if(q.ContainsKey("developer"))
            {
                string developer_id = q["developer"];
                if(q.ContainsKey("filterDeveloper"))
                {
                    if(q["filterDeveloper"] == "true")
                    {
                        tags.Add("developer_" + developer_id);
                        outMessages.Add(new AppstoreListUiMessage("You're viewing apps by a single developer."));
                    }
                }
            }

            //Convert this to a query object.
            //Get the page offset by dividing the offset by limit.
            page_offset = offset / limit;
            return CreateQuery(searchQuery, tags.ToArray(), limit, page_offset);
        }

        /// <summary>
        /// Create an Algolia query object.
        /// </summary>
        public static Query CreateQuery(string query, string[] tags, int hitsPerPage, int offset)
        {
            Query q = new Query(query);
            //Add tag filters, such as "watchface" or "aplite"
            q.SetTagFilters(ConvertTagsString(tags));
            //Add limit and offset.
            q.SetNbHitsPerPage(hitsPerPage);
            q.SetPage(offset);

            return q;
        }

        /// <summary>
        /// Send the request to Algolia and get replies.
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static async Task<Newtonsoft.Json.Linq.JObject> GetAlgoliaReplyAsync(Query q)
        {
            return await AlgoliaApi.GetIndex().SearchAsync(q);
        }

        private static string ConvertTagsString(string[] tags)
        {
            string t = "";
            for (int i = 0; i < tags.Length; i++)
            {
                t += tags[i];
                if (i + 1 != tags.Length)
                    t += ",";
            }
            return t;
        }

        private class Endpoint_SearchPage
        {
            public int length;
            public int total;
            public int totalPages;
            public Endpoint_SearchPageCan can;
            public List<AppstoreConvertedApp> data;
            public bool maintenance;
            public string maintenance_msg;
            public string html;
        }

        private class Endpoint_SearchPageCan
        {
            public bool back;
            public bool next;
        }
    }

    public class AppstoreListUiMessage
    {
        public string html;

        public AppstoreListUiMessage(string innerHtml, string color = "f0f0f0")
        {
            html = $"<ul class=\"appsList appListUl\" style=\"background-color: #{color};text-align:center;padding: 10px;font-family: 'd-din';\">{innerHtml}</ul>";
        }
    }
}
