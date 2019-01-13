using LibRpws;
using LibRpws2.Entities;
using RpwsBlue.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace RpwsBlue.Services.PublishApi.PublishServices
{
    public class EditCompanions
    {
        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee, PebbleAppDbStorage app)
        {
            //Get method
            RequestHttpMethod method = Program.FindRequestMethod(e);

            //Open payload
            EditCompanionsRequestPayload payload = Program.GetPostBodyJson<EditCompanionsRequestPayload>(e);

            EditCompanionsReply reply;

            if (method == RequestHttpMethod.put)
            {
                reply = OnPutRequest(e, ee, app, payload);
            }
            else if (method == RequestHttpMethod.delete)
            {
                reply = OnDeleteRequest(e, ee, app, payload);
            }
            else
            {
                throw new RpwsStandardHttpException("Invalid method");
            }

            Program.QuickWriteJsonToDoc(e, reply);
        }

        private static ApptweakReply PollAppInfo(string name, string platform)
        {
            ApptweakReply replyApptweak;
            try
            {
                replyApptweak = LibRpws.LibRpwsCore.GetObjectHttp<ApptweakReply>("https://api.apptweak.com/" + platform + "/applications/" +  name + "/information.json?country=de&language=en&device=iphone", null, new Dictionary<string, string>()
                {
                    {"X-Apptweak-Key",Program.config.secure_creds["apptweak"]["apiKey"] },
                    {"Accept","*/*" }
                });
            }
            catch 
            {
                return null;
            }
            return replyApptweak;
        }

        private static EditCompanionsReply OnPutRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee, PebbleAppDbStorage app, EditCompanionsRequestPayload payload)
        {
            //Query app info
            ApptweakReply appInfo = PollAppInfo(payload.url, "android");

            if(appInfo == null)
            {
                return new EditCompanionsReply
                {
                    message = "Failed to find app. Check the URL.",
                    ok = false
                };
            }

            //Download the icon image.
            string icon_image;
            using(MemoryStream ms = new MemoryStream())
            {
                WebRequest.Create(appInfo.content.icon).GetResponse().GetResponseStream().CopyTo(ms);
                ms.Position = 0;
                icon_image = UserContentUploader.UploadFile(ms).GetAwaiter().GetResult();
            }

            //Create the data
            CompanionPlatform c = new CompanionPlatform
            {
                icon = icon_image,
                id = LibRpws.LibRpwsCore.GenerateRandomHexString(24),
                name = appInfo.content.title,
                pebblekit_version = "1", /* Unknown */
                required = payload.required,
                url = $"https://play.google.com/store/apps/details?id={appInfo.metadata.request.@params.id}"
            };

            //Add it to the app
            app.app.companions.android = c;
            CorePublishApi.SaveApp(app);

            return new EditCompanionsReply
            {
                message = $"Added {appInfo.content.title}!",
                ok = true
            };
        }

        private static EditCompanionsReply OnDeleteRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee, PebbleAppDbStorage app, EditCompanionsRequestPayload payload)
        {
            app.app.companions.android = null;
            CorePublishApi.SaveApp(app);

            return new EditCompanionsReply
            {
                message = $"Companion app removed.",
                ok = true
            };
        }
    }

    class EditCompanionsReply
    {
        public bool ok;
        public string message;
    }

    class EditCompanionsRequestPayload
    {
        public string url;
        public bool required;
    }
}
