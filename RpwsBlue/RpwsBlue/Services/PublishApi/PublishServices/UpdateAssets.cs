using LibRpws;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using LibRpws2.Entities;

namespace RpwsBlue.Services.PublishApi.PublishServices
{
    public static class UpdateAssets
    {
        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee, PebbleAppDbStorage app)
        {
            //Get vars
            RequestHttpMethod method = Program.FindRequestMethod(e);
            AssetType type = (AssetType)int.Parse(e.Request.Query["asset_type"]);

            //Decide what to do.
            try
            {
                if (type == AssetType.Header && method == RequestHttpMethod.put)
                    OnHeaderPut(e, ee, app);
                if (type == AssetType.Header && method == RequestHttpMethod.delete)
                    OnHeaderDelete(e, ee, app);
                if (type == AssetType.Screenshot && method == RequestHttpMethod.put)
                    OnScreenshotPut(e, ee, app);
                if (type == AssetType.Screenshot && method == RequestHttpMethod.delete)
                    OnScreenshotDelete(e, ee, app);
                if (type == AssetType.List && method == RequestHttpMethod.put)
                    OnListPut(e, ee, app);
                if (type == AssetType.List && method == RequestHttpMethod.delete)
                    OnListDelete(e, ee, app);
            } catch (Exception ex)
            {
                //Write error.
                Program.QuickWriteJsonToDoc(e, new RSHE_Output
                {
                    title = "Error uploading - " + ex.Message,
                    retryable = true,
                    message = "Error uploading - " + ex.Message
                }, 500);
                return;
            }

            //Finally, show the app info refreshed.
            PublishServices.AppInfo.OnRequest(e, ee, app);
        }

        private static string GetAndProcessImage(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Get file
            var f = e.Request.Form.Files["file"];

            //Open buffer and copy to it.
            byte[] buf = new byte[f.Length];
            f.OpenReadStream().Read(buf, 0, buf.Length);

            //Upload this to the server.
            string url = UserContentUploader.UploadAndProcessImage(buf).GetAwaiter().GetResult();
            return url;
        }

        /// <summary>
        /// Get index from URL.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private static int GetIndex(Microsoft.AspNetCore.Http.HttpContext e)
        {
            return int.Parse(e.Request.Query["index"]);
        }

        private static void OnHeaderPut(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee, PebbleAppDbStorage app)
        {
            //Upload and obtain a URL.
            string url = GetAndProcessImage(e);

            //Add new entry into array.
            if (app.app.header_images == null)
                app.app.header_images = new List<PebbleApp_HeaderImg>();
            app.app.header_images.Add(new PebbleApp_HeaderImg
            {
                orig = url
            });

            //Save
            CorePublishApi.SaveApp(app);
        }

        private static void OnHeaderDelete(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee, PebbleAppDbStorage app)
        {
            //Modify array.
            app.app.header_images.RemoveAt(GetIndex(e));

            //Save
            CorePublishApi.SaveApp(app);
        }

        private static void OnScreenshotPut(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee, PebbleAppDbStorage app)
        {
            //Upload and obtain a URL.
            string url = GetAndProcessImage(e);

            //Create entry
            PebbleApp_ScreenshotImg img = new PebbleApp_ScreenshotImg
            {
                _144x168 = url,
                _180x180 = url
            };

            //Add new entry into array.
            var keys = new string[] { "aplite", "basalt", "chalk", "diorite", "emery" };
            foreach (var p in keys)
            {
                if (!app.app.screenshot_images.ContainsKey(p))
                    app.app.screenshot_images.Add(p, new List<PebbleApp_ScreenshotImg>());
                app.app.screenshot_images[p].Add(img);
            }

            //Save
            CorePublishApi.SaveApp(app);
        }

        private static void OnScreenshotDelete(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee, PebbleAppDbStorage app)
        {
            //Get index
            int index = GetIndex(e);

            //Delete enteries in array.
            foreach (var p in app.app.screenshot_images.Keys)
                app.app.screenshot_images[p].RemoveAt(index);

            //Save
            CorePublishApi.SaveApp(app);
        }

        private static void OnListPut(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee, PebbleAppDbStorage app)
        {
            //This deals with both icon images and list images.
            
            //Upload and obtain a URL.
            string url = GetAndProcessImage(e);

            //Create list image
            var list_img = new PebbleApp_ListImg
            {
                _144x144 = url
            };

            //Create icon images
            var icon_image = new PebbleApp_IconImg
            {
                _48x48 = url
            };

            //Set list images
            app.app.list_image = new Dictionary<string, PebbleApp_ListImg>
            {
                { "aplite", list_img },
                { "basalt", list_img },
                { "chalk", list_img },
                { "diorite", list_img },
                { "emery", list_img }
            };

            //Set icon images
            app.app.icon_image = new Dictionary<string, PebbleApp_IconImg>
            {
                { "aplite", icon_image },
                { "basalt", icon_image },
                { "chalk", icon_image },
                { "diorite", icon_image },
                { "emery", icon_image  }
            };

            //Save
            CorePublishApi.SaveApp(app);
        }

        private static void OnListDelete(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee, PebbleAppDbStorage app)
        {
            //Set both list images and icon images to null
            app.app.list_image = null;
            app.app.icon_image = null;

            //Save
            CorePublishApi.SaveApp(app);
        }

        enum AssetType
        {
            Screenshot,
            Header,
            List
        }
    }
}
