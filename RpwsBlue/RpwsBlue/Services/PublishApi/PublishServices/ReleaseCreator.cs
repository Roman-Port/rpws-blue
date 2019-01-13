using LibRpws;
using Newtonsoft.Json;
using RpwsBlue.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using static RpwsBlue.Services.PublishApi.PublishServices.AppInfo;
using LibRpws2.Entities;

namespace RpwsBlue.Services.PublishApi.PublishServices
{
    public static class ReleaseCreator
    {
        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee, PebbleAppDbStorage app)
        {
            //First, open the PBW file.
            var f = e.Request.Form.Files["file"];
            MemoryStream ms = new MemoryStream();
            f.CopyTo(ms);
            ms.Position = 0;

            PbwInfo pbwinfo;

            try
            {
                pbwinfo = ReadPbwMetadata(ms, app.app.uuid, out string new_uuid);
                app.app.uuid = new_uuid;
            } catch (InvalidDataException ide)
            {
                //Error with validation.
                Program.QuickWriteJsonToDoc(e, new ReleaseCreatorOutput
                {
                    ok = false,
                    headerText = "Invalid PBW File",
                    message = "Failed to open PBW as ZIP. It was invalid.",
                    appId = app.app.id
                });
                return;
            } catch (ReleaseValidationError ex)
            {
                //Error with validation.
                Program.QuickWriteJsonToDoc(e, new ReleaseCreatorOutput
                {
                    ok = false,
                    headerText = "PBW Validation Error",
                    message = ex.message,
                    helpUrl = ex.helpUrl,
                    appId = app.app.id
                });
                return;
            }

            //Upload the PBW file.
            ms.Position = 0;
            string url = UserContentUploader.UploadFile(ms, "application/octet-stream").GetAwaiter().GetResult();

            string releaseNotes = e.Request.Form["notes"];

            //Create release data.
            PebbleApp_Release release = new PebbleApp_Release();
            release.id = LibRpwsCore.GenerateRandomHexString(24);
            release.js_version = 1; //Todo: Check if this is correct.
            release.pbw_file = url;
            release.published_date = JsonConvert.SerializeObject(DateTime.UtcNow).Trim('"');
            release.release_notes = releaseNotes;
            release.version = pbwinfo.appinfo.versionLabel;

            //Add to release history.
            var releaseHistory = app.dev.release_history.ToList();
            releaseHistory.Add(release);
            app.dev.release_history = releaseHistory.ToArray();

            //Add changelog.
            var cl = new PebbleApp_ChangelogItem();
            cl.published_date = JsonConvert.SerializeObject(DateTime.UtcNow);
            cl.release_notes = System.Web.HttpUtility.HtmlEncode(releaseNotes);
            cl.version = release.version;
            var cll = app.app.changelog.ToList();
            cll.Insert(0, cl);
            app.app.changelog = cll.ToArray();

            //Set all. These are seperate for compatibility reasons.
            app.app.latest_release = release;

            //Now, we'll set compatbility. First, set all to incompatible.
            PebbleApp_Compatibility_Hardware c_unsupported = new PebbleApp_Compatibility_Hardware();
            c_unsupported.supported = false;
            PebbleApp_Compatibility_Hardware c_good = new PebbleApp_Compatibility_Hardware();
            c_good.supported = true;
            c_good.firmware = new PebbleApp_Compatibility_Hardware_Firmware();
            c_good.firmware.major = int.Parse(pbwinfo.appinfo.sdkVersion);

            //Set all to incompatible.
            app.app.compatibility.aplite = c_unsupported;
            app.app.compatibility.basalt = c_unsupported;
            app.app.compatibility.chalk = c_unsupported;
            app.app.compatibility.diorite = c_unsupported;
            app.app.compatibility.emery = c_unsupported;

            //Set each. This is gross and janky
            foreach (string platform in pbwinfo.valid_platforms)
            {
                switch (platform)
                {
                    case "aplite":
                        app.app.compatibility.aplite = c_good;
                        break;
                    case "basalt":
                        app.app.compatibility.basalt = c_good;
                        break;
                    case "chalk":
                        app.app.compatibility.chalk = c_good;
                        break;
                    case "diorite":
                        app.app.compatibility.diorite = c_good;
                        break;
                    case "emery":
                        app.app.compatibility.emery = c_good;
                        break;
                }
            }

            //Set the metadata

            if (app.app.table_meta == null)
            {
                //Create new table metadata
                app.app.table_meta = new TableMeta
                {
                    appinfo = null,
                    capabilities = PebbleApp_Capabilities.ConvertFromAppinfo(pbwinfo.appinfo.capabilities),
                    isDeleted = false,
                    isOriginal = false,
                    isPublished = false,
                    isTimelineKnownUnsupported = false,
                    platform_tags = pbwinfo.valid_platforms,
                    table_version = 6,
                    pbw_info = pbwinfo
                };
            }

            //Save
            CorePublishApi.SaveApp(app);

            //Create output.
            Program.QuickWriteJsonToDoc(e, new ReleaseCreatorOutput
            {
                ok = true,
                updated_data = AppInfo.ConvertAppToReply(app),
                label = pbwinfo.appinfo.versionLabel,
                appId = app.app.id
            });
            return;
        }

        public static PbwInfo ReadPbwMetadata(MemoryStream ms, string originalUuid, out string new_uuid)
        {
            new_uuid = originalUuid;
            PbwInfo o = new PbwInfo();
            //Start the validation process.
            using (ZipArchive z = new ZipArchive(ms, ZipArchiveMode.Read, true))
            {
                //Get the app info.
                o.appinfo = ReadJsonFromZip<PbwAppInfo2>(z, "appinfo.json");
                o.platform_info = new Dictionary<string, PbwPlatformInfo>();

                //Manually find the files for extracting platform data
                string[] possible_platforms = new string[] { "aplite", "basalt", "chalk", "diorite", "emery" };
                List<string> valid_platforms = new List<string>();
                foreach (string s in possible_platforms)
                {
                    if (z.GetEntry(s + "/pebble-app.bin") != null)
                        valid_platforms.Add(s);
                }
                //Aplite sometimes ignores the folder structure (even if there are other folders) and uses the root, probably for backwards compatability.
                if (z.GetEntry("pebble-app.bin") != null)
                {
                    //Aplite is the only known one.
                    valid_platforms.Add("aplite");
                }
                
                //Check if any valid platforms were found.
                if(valid_platforms.Count == 0)
                    throw new ReleaseValidationError("Could not find required file pebble-app.bin.");

                o.valid_platforms = valid_platforms.ToArray();

                //Loop through each of the target platforms and extract the metadata.
                foreach (string platform in valid_platforms)
                {
                    //Find the manifest file. In some versions of the PBW structure, this is at the root. Usually, however, it is located in the folder for the platform.
                    PebbleAppManifest m = FindManifiestData(z, platform, out string path);
                    var bin = OpenBinaryFile(z, m, path);
                    AppMeta2 platformAppMeta = ExtractMetadata(bin);

                    //Validate the metadata.
                    ValidateMetadata(platformAppMeta, originalUuid, o.appinfo, out new_uuid);

                    //Insert into dict
                    o.platform_info.Add(platform, new PbwPlatformInfo
                    {
                        appmeta = platformAppMeta,
                        manifest = m
                    });
                }
            }

            return o;
        }

        private static MemoryStream FindAndReadEntry(ZipArchive z, string name)
        {
            MemoryStream outStream = new MemoryStream();
            try
            {
                var f = z.GetEntry(name);
                Stream fs = f.Open();
                fs.CopyTo(outStream);
                outStream.Position = 0;
            }
            catch
            {
                throw new ReleaseValidationError($"Failed to open required file, '{name}'.");
            }
            return outStream;
        }

        /// <summary>
        /// Opens a entry in the ZipStream and decodes it as JSON.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="z"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static T ReadJsonFromZip<T>(ZipArchive z, string name)
        {
            string fstring;
            T data;

            MemoryStream ms = FindAndReadEntry(z, name);
            byte[] buf = new byte[ms.Length];
            ms.Read(buf, 0, buf.Length);
            fstring = Encoding.UTF8.GetString(buf);

            try
            {
                data = JsonConvert.DeserializeObject<T>(fstring);
            } catch
            {
                Console.WriteLine(fstring);
                throw new ReleaseValidationError($"Failed to deserialize required file, '{name}', into type, '{typeof(T).AssemblyQualifiedName}'.");
            }
            if(data == null)
                throw new ReleaseValidationError($"Deserialization of required file, '{name}', resulted in a null type.");
            return data;
        }

        /// <summary>
        /// Find pebble-app.bin as well as the other data.
        /// </summary>
        /// <param name="z"></param>
        /// <param name="platformName"></param>
        private static PebbleAppManifest FindManifiestData(ZipArchive z, string platformName, out string path)
        {
            // Look in the resource folder first.
            path = null;
            string[] pathsToTry = new string[] { "", $"{platformName}/" };
            foreach(string s in pathsToTry)
            {
                if(z.GetEntry(s + "manifest.json") != null)
                {
                    //Valid path.
                    path = s;
                    break;
                }
            }

            //Check if ok
            if (path == null)
                throw new Exception($"Failed to find required file, manifest.json.");

            //Open the manifest and return it.
            PebbleAppManifest m = ReadJsonFromZip<PebbleAppManifest>(z, path + "manifest.json");
            return m;
        }

        private static ZipArchiveEntry OpenBinaryFile(ZipArchive z, PebbleAppManifest m, string path)
        {
            //Look in the resource folder first.
            ZipArchiveEntry f;
            f = z.GetEntry(path + m.application.name);
            if (f == null)
                throw new ReleaseValidationError($"Could not find required Pebble binary file from manifest.");
            return f;
        }

        private static AppMeta2 ExtractMetadata(ZipArchiveEntry ze)
        {
            //Copy file contents.
            Stream ms = ze.Open();
            AppMeta2 m = MetadataExtractor.ExtractMetadata(ms);
            ms.Close();
            return m;
        }

        private static void ValidateMetadata(AppMeta2 m, string originalUuid, PbwAppInfo2 i, out string new_uuid)
        {
            //If UUID is null, set it to empty for the next check.
            if (originalUuid == null)
                originalUuid = "";
            //Check if the UUID from the binary matches the UUID from the appinfo.json file.
            if (m.uuid != i.uuid.ToLower())
                throw new ReleaseValidationError($"UUID from appinfo.json, '{i.uuid.ToLower()}', does not match UUID from binary, '{m.uuid}'.");
            //Check if the UUID from the binary matches the existing UUID if there is an existing UUID.
            if (m.uuid != originalUuid.ToLower() && originalUuid.Length!=0)
                throw new ReleaseValidationError($"New UUID, '{m.uuid}', does not match the old UUID, '{originalUuid}'.");

            new_uuid = m.uuid;
        }
    }

    class ReleaseValidationError : Exception
    {
        public string message;
        public string helpUrl;

        public ReleaseValidationError(string message, string helpUrl = null)
        {
            this.message = message;
            this.helpUrl = helpUrl;
        }
    }

    class ReleaseCreatorOutput
    {
        public bool ok;
        public string appId;

        public string headerText;
        public string message;
        public string helpUrl;

        public AppInfoReply updated_data;
        public string label;
    }

    /* Some gross code for reading the binary header from May 2018 */
    class MetadataExtractor
    {
        public static AppMeta2 ExtractMetadata(Stream stream)
        {
            //Point this at pebble-app.bin.
            AppMeta2 meta = new AppMeta2();
            meta.header = BytesToString(ExtractBytesFromStream(stream, 8)); //0
            meta.struct_version = BytesToVersion(ExtractBytesFromStream(stream, 2)); //8
            meta.sdk_version = BytesToVersion(ExtractBytesFromStream(stream, 2)); //10
            meta.app_version = BytesToVersion(ExtractBytesFromStream(stream, 2)); //12
            meta.size = BytesToUInt16(ExtractBytesFromStream(stream, 2)); //14
            meta.offset = BytesToSInt32(ExtractBytesFromStream(stream, 4)); //16
            meta.crc = BytesToSInt32(ExtractBytesFromStream(stream, 4)); //20
            meta.appname = BytesToString(ExtractBytesFromStream(stream, 32)); //24
            meta.companyname = BytesToString(ExtractBytesFromStream(stream, 32)); //56
            meta.icon_resource_id = BytesToUInt32(ExtractBytesFromStream(stream, 4)); //98
            meta.symbol_table_address = BytesToUInt32(ExtractBytesFromStream(stream, 4)); //102
            meta.pebble_process_info_flags = BytesToUInt32(ExtractBytesFromStream(stream, 4)); //106
            meta.relocation_list = BytesToUInt32(ExtractBytesFromStream(stream, 4)); //110
            byte[] uuid = ExtractBytesFromStream(stream, 16);
            meta.uuid = "";
            for (int i = 0; i<16; i++)
            {
                meta.uuid += uuid[i].ToString("X2").ToLower();
                if (i == 3 || i == 5 || i == 7 || i == 9)
                    meta.uuid += "-";
            }

            return meta;
        }



        public static int BytesToSInt32(byte[] buf)
        {
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(buf);
            return BitConverter.ToInt32(buf, 0);
        }

        public static int BytesToSInt16(byte[] buf)
        {
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(buf);
            return BitConverter.ToInt16(buf, 0);
        }

        public static int BytesToUInt16(byte[] buf)
        {
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(buf);
            return BitConverter.ToUInt16(buf, 0);
        }

        public static long BytesToUInt32(byte[] buf)
        {
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(buf);
            return BitConverter.ToUInt32(buf, 0);
        }

        public static string BytesToString(byte[] buf)
        {
            return Encoding.UTF8.GetString(buf);
        }

        public static char BytesToChar(byte[] buf)
        {
            return BitConverter.ToChar(buf, 0);
        }

        public static string BytesToVersion(byte[] buf)
        {
            //Two 8-bit ints smooshed together with a poor period in the middle.
            int begin = BytesToUInt16(new byte[] { buf[0], 0 });
            int end = BytesToUInt16(new byte[] { buf[1], 0 });
            return begin.ToString() + "." + end.ToString();
        }

        private static byte[] ExtractBytesFromStream(Stream stream, int length)
        {
            byte[] buf = new byte[length];
            stream.Read(buf, 0, length);

            return buf;
        }
    }
}
