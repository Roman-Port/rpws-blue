using LibRpws2.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services.Locker
{
    public class LockerAppFormat
    {
        public string id;
        public string uuid;
        public string user_token;
        public string title;
        public string type;
        public string category;
        public string version;
        public string hearts;
        public bool is_configurable;
        public bool is_timeline_enabled;
        public LockerAppFormatLinks links;
        public LockerAppFormatDeveloper developer;
        public LockerAppFormatPbw pbw;
        public LockerAppFormatHardwarePlatform[] hardware_platforms;
        public PebbleApp_Compatibility compatibility;
        public PebbleApp_Companions companions;

        public LockerAppFormat()
        {

        }

        public LockerAppFormat(AppstoreApp src, string token, int voteTotal)
        {
            id = src.id;
            uuid = src.uuid;
            user_token = token;
            title = src.title;
            type = src.type;
            category = src.category_name;
            hearts = voteTotal.ToString();

            links = new LockerAppFormatLinks(src);
            developer = new LockerAppFormatDeveloper(src);
            pbw = new LockerAppFormatPbw(src, out version);
            //Create hardware platforms.
            List<LockerAppFormatHardwarePlatform> h = new List<LockerAppFormatHardwarePlatform>();
            foreach(var k in src.screenshot_images)
            {
                string name = k.Key;
                if (src.list_image.ContainsKeyNotNull(name) && src.compatibility.GetHardwareByName(name).supported)
                    h.Add(new LockerAppFormatHardwarePlatform(src, src.list_image[name], k.Value, name));
            }
            hardware_platforms = h.ToArray();
            compatibility = src.compatibility;
            companions = src.companions;

            //Set the abilities.
            if (src.table_meta.capabilities == null)
            {
                //Capabilities UNKNOWN!
                //Use the default of "true" for each
                is_configurable = true;
                is_timeline_enabled = true;
            }
            else
            {
                //Use options
                is_configurable = src.table_meta.capabilities.is_configurable == 1;
                is_timeline_enabled = src.table_meta.capabilities.is_timeline_enabled == 1;
            }
        }
    }

    public class LockerAppFormatLinks
    {
        public string remove;
        public string href;
        public string share;

        public LockerAppFormatLinks(AppstoreApp src)
        {
            remove = "https://" + LibRpws.LibRpwsCore.config.public_host + "/v1/locker/" + src.uuid + "/";
            href = "https://"+LibRpws.LibRpwsCore.config.public_host + "/v1/locker/" + src.uuid + "/";
            share = "https://app.get-rpws.com/" + src.id;
        }

        public LockerAppFormatLinks()
        {

        }
    }

    public class LockerAppFormatDeveloper
    {
        public string contact_email;
        public string id;
        public string name;

        public LockerAppFormatDeveloper(AppstoreApp src)
        {
            contact_email = ""; //RIP 
            id = src.developer_id;
            name = src.author;
        }

        public LockerAppFormatDeveloper()
        {

        }
    }

    public class LockerAppFormatPbw
    {
        public int icon_resource_id;
        public string file;
        public string release_id;

        public LockerAppFormatPbw()
        {

        }

        public LockerAppFormatPbw(AppstoreApp src, out string version)
        {
            //Find a working PBW.
            PebbleApp_Release release = src.latest_release;
            file = release.pbw_file;
            release_id = release.id;
            icon_resource_id = int.Parse(src.table_meta.pbw_info.TryGetAppMeta().appmeta.icon_resource_id.ToString());
            version = release.version;
        }


    }

    public class LockerAppFormatHardwarePlatform
    {
        public string sdk_version;
        public int pebble_process_info_flags;
        public string description;
        public LockerAppFormatHardwarePlatformImages images;
        public string name; //Platform name, I.E. basalt

        public LockerAppFormatHardwarePlatform(AppstoreApp src, PebbleApp_ListImg listImg, List<PebbleApp_ScreenshotImg> screenshotImgs, string platformName)
        {
            name = platformName;
            var appmeta = src.table_meta.pbw_info.TryGetAppMeta(platformName).appmeta;

            var sdk_version_info = appmeta.sdk_version;
            sdk_version = sdk_version_info;
            pebble_process_info_flags = (int)appmeta.pebble_process_info_flags;
            description = src.description;
            images = new LockerAppFormatHardwarePlatformImages();
            images.list = listImg._144x144;
            if (screenshotImgs[0]._144x168 != null)
            {
                images.screenshot = screenshotImgs[0]._144x168;
            }
            else if (screenshotImgs[0]._180x180 != null)
            {
                images.screenshot = screenshotImgs[0]._180x180;
            }
            else
            {
                images.screenshot = "https://romanport.com/static/Pebble/watchface_placeholder_icon.png";
            }

        }

        public LockerAppFormatHardwarePlatform()
        {

        }
    }

    public class LockerAppFormatHardwarePlatformImages
    {
        public string screenshot;
        public string list;
    }
}
