using LibRpws;
using LibRpws2.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RpwsBlue.Services.PublishApi
{
    public static class CreateApp
    {
        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession eee)
        {
            if (CorePublishApi.SetRequestHeaders(e))
                return;
            //Make sure this is a post request.
            if (Program.FindRequestMethod(e) != RequestHttpMethod.post)
                throw new RpwsStandardHttpException("Invalid method.");
            //Read in the POST data.
            CreateAppRequestData request = Program.GetPostBodyJson<CreateAppRequestData>(e);
            //Do some validation.
            if (request.name.Length > 20 || request.name.Length < 4)
                throw new RpwsStandardHttpException("The title of your app must be between 4-20 characters.", false);
            if (CorePublishApi.collection.Find(x => x.app.title == request.name).Count() != 0)
                throw new RpwsStandardHttpException($"You already own an app with that name.", false);
            if (!eee.user.isAppDev)
                throw new RpwsStandardHttpException($"You aren't an app developer! Register before submitting an app.", true);
            //Create an object for this.
            AppstoreApp a = new AppstoreApp();
            //Determine categories
            switch (request.category_index)
            {
                case "0":
                    a.category_id = "5261a8fb3b773043d500000c";
                    a.category_name = "Daily";
                    a.category_color = "edb9e6";
                    break;
                case "1":
                    a.category_id = "5261a8fb3b773043d500000f";
                    a.category_name = "Tools & Utilities";
                    a.category_color = "fdbf37";
                    break;
                case "2":
                    a.category_id = "5261a8fb3b773043d5000001";
                    a.category_name = "Notifications";
                    a.category_color = "FF9000";
                    break;
                case "3":
                    a.category_id = "5261a8fb3b773043d5000008";
                    a.category_name = "Remotes";
                    a.category_color = "fc4b4b";
                    break;
                case "4":
                    a.category_id = "5261a8fb3b773043d5000004";
                    a.category_name = "Health & Fitness";
                    a.category_color = "98D500";
                    break;
                case "5":
                    a.category_id = "5261a8fb3b773043d5000012";
                    a.category_name = "Games";
                    a.category_color = "b57ad3";
                    break;
                case "6":
                    a.category_id = "528d3ef2dc7b5f580700000a";
                    a.category_name = "Faces";
                    a.category_color = "FFFFFF";
                    break;
                default:
                    throw new RpwsStandardHttpException("Unknown category ID.", false);
            }
            if(request.type == CreateAppRequestData.CreateAppRequestData_AppType.watchapp && a.category_id == "528d3ef2dc7b5f580700000a")
            {
                throw new RpwsStandardHttpException("You tried to submit a watchapp with a watchface category.");
            }
            if (request.type == CreateAppRequestData.CreateAppRequestData_AppType.watchface && a.category_id != "528d3ef2dc7b5f580700000a")
            {
                throw new RpwsStandardHttpException("You tried to submit a watchface with a watchapp category.");
            }
            a.author = eee.user.appDevName;
            a.capabilities = new PebbleApp_Capabilities();
            a.capabilities.is_configurable = 0;
            a.capabilities.is_timeline_enabled = 0;
            a.changelog = new PebbleApp_ChangelogItem[0];
            a.companions = new PebbleApp_Companions();
            a.companions.android = null;
            a.companions.ios = null;
            a.compatibility = new PebbleApp_Compatibility();
            a.compatibility.android = new PebbleApp_Compatibility_Phone();
            a.compatibility.android.supported = true;
            a.compatibility.ios = new PebbleApp_Compatibility_Phone();
            a.compatibility.ios.supported = true;
            var h = new PebbleApp_Compatibility_Hardware();
            h.supported = false;
            a.compatibility.aplite = h;
            a.compatibility.basalt = h;
            a.compatibility.chalk = h;
            a.compatibility.diorite = h;
            a.compatibility.emery = h;
            a.created_at = JsonConvert.SerializeObject(DateTime.UtcNow);
            a.description = "";
            a.developer_id = eee.user.pebbleId;
            a.header_images = new List<PebbleApp_HeaderImg>();
            a.isOriginal = 0;
            a.isPublished = 0;
            a.isTimelineKnownUnsupported = 0;
            a.latest_release = null; //This'll be set when a PBW is added.
            a.links = new PebbleApp_Links();
            a.links.add = "";
            a.links.add_flag = "";
            a.links.add_heart = "";
            a.links.remove = "";
            a.links.remove_flag = "";
            a.links.remove_heart = "";
            a.links.share = "https://app.get-rpws.com/" + a.id;
            a.list_image = new Dictionary<string, PebbleApp_ListImg>();
            a.meta = null; //This'll be set when a PBW is added.
            a.published_date = a.created_at;
            a.screenshot_images = new Dictionary<string, List<PebbleApp_ScreenshotImg>>();
            a.source = "";
            a.title = System.Web.HttpUtility.HtmlEncode(request.name);
            a.type = request.type.ToString();
            a.uuid = null; //This'll be set when a PBW is added.
            a.website = "";
            //Generate an ID.
            a.id = LibRpwsCore.GenerateRandomHexString(24);
            while(CorePublishApi.collection.Find( x => x.app.id == a.id).Count() != 0)
                a.id = LibRpwsCore.GenerateRandomHexString(24);
            //Add to the database.
            PebbleAppDbStorage s = new PebbleAppDbStorage();
            s.app = a;
            s.dbVersion = 4;
            s.dev = new PebbleApp_RPWS_Dev();
            s.dev.changes_pending = true;
            s.dev.release_history = new PebbleApp_Release[0];
            s.dev.last_edit_time = DateTime.UtcNow.Ticks;

            //Insert into the collection.
            CorePublishApi.collection.Insert(s);

            //Add an entry for likes
            AppstoreVoteApi.AddAppEntry(a.id, 0);

            //Respond with the app data.
            Program.QuickWriteJsonToDoc(e, new CreateAppReplyData
            {
                ok = true,
                id = a.id
            });
        }

        class CreateAppRequestData
        {
            public string name;
            public string category_index;
            public CreateAppRequestData_AppType type;

            public enum CreateAppRequestData_AppType
            {
                watchapp,
                watchface
            }
        }

        class CreateAppReplyData
        {
            public bool ok;
            public string id;
        }
    }
}
