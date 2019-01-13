using LibRpws;
using LibRpws2.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services.PublishApi.PublishServices
{
    public static class AppInfo
    {
        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee, PebbleAppDbStorage app)
        {
            Program.QuickWriteJsonToDoc(e, ConvertAppToReply(app));
        }

        public static AppInfoReply ConvertAppToReply(PebbleAppDbStorage app)
        {
            //Convert the app to the reply format.
            AppInfoReply reply = new AppInfoReply();
            
            reply.name = app.app.title;
            reply.uuid = app.app.uuid;
            reply.type = app.app.type;
            reply.id = app.app.id;
            if(app.dbVersion < 6)
            {
                //Upgrade.
                app.dev.is_published = app.app.isPublished == 1;
                app.dev.last_release_time = -1;
                app.dev.changes_pending = true;
                app.dev.changes_since_last_published = 0;
                CorePublishApi.SaveApp(app);
            }
            reply.isPublished = app.dev.is_published;

            reply.description = app.app.description;
            reply.source = app.app.source;
            reply.website = app.app.website;

            reply.isTimelineEnabled = false;
            
            reply.screenshots = app.app.screenshot_images.GetItem(false);
            if (reply.screenshots == null)
                reply.screenshots = new List<PebbleApp_ScreenshotImg>();
            reply.icon_image = app.app.icon_image.GetItem(false);
            reply.list_image = app.app.list_image.GetItem(false);
            reply.header_images = app.app.header_images;
            if (reply.header_images == null)
                reply.header_images = new List<PebbleApp_HeaderImg>();

            reply.releases = app.dev.release_history;
            foreach (var r in reply.releases)
            {
                r.published_date = r.published_date.Trim('"');
            }

            reply.companions = app.app.companions;

            reply.status = CheckAppReadyStatus.CheckApp(app);

            return reply;
        }

        public class AppInfoReply
        {
            public string name;
            public string uuid;
            public string type;
            public bool isPublished;
            public string id;

            public string description;
            public string source;
            public string website;

            public bool isTimelineEnabled;

            public List<PebbleApp_ScreenshotImg> screenshots;
            public PebbleApp_IconImg icon_image;
            public PebbleApp_ListImg list_image;
            public List<PebbleApp_HeaderImg> header_images;

            public PebbleApp_Release[] releases;

            public PebbleApp_Companions companions;

            public AppReadyStatus status;
        }
    }
}
