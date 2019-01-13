using LibRpws2.Entities;
using Newtonsoft.Json;
using RpwsBlue.Services.PublishApi.PublishServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RpwsBlue.Migrations
{
    class AppstoreMigration
    {
        public static void RunMigration()
        {
            Console.WriteLine("About to do migration. This could trash the datbase if there is existing data. Press ENTER to begin.");
            Console.ReadLine();
            Console.WriteLine(Services.Locker.LockerService.CheckIfAppIsPbwPatched("539e18f21a19dec6ca0000aa"));
            var transact = AppstoreApi.GetTransaction();
            List<string> used_uuids = new List<string>();
            JsonSerializerSettings sett = new JsonSerializerSettings();
            //sett.MissingMemberHandling = MissingMemberHandling.Ignore;
            //Keep requesting pages.
            Console.Write("\nMigrating Apps... (awaiting total)");
            int total = -1;
            int done = 0;
            string next = "https://pebble-appstore.romanport.com/api/raw/stream/index_dict.php?offset=0&auth=xUENomjl1JdNFxH5ZM83hcVvFFeMEiuV";
            List<AppstoreApp> appsToAdd = new List<AppstoreApp>();
            while (next != null)
            {
                var page = RunAppHttpApi(sett, next);
                total = page.total;
                next = page.next;
                //Loop through apps
                Random rand = new Random();
                int patchedCount = 0;
                Parallel.For(0, page.data.Length, (int i) =>
                {
                    var a = page.data[i];

                    Console.Write("\rMigrating Apps... [" + done.ToString() + " / " + total.ToString() + " ("+ patchedCount.ToString() +" patched)]              ");
                    done++;
                    //Generate the table meta.
                    a.table_meta = new TableMeta();
                    a.table_meta.capabilities = a.capabilities;
                    a.table_meta.isDeleted = false;
                    a.table_meta.isOriginal = a.isOriginal == 1;
                    a.table_meta.isPublished = a.isPublished == 1;
                    a.table_meta.isTimelineKnownUnsupported = a.isTimelineKnownUnsupported == 1;
                    a.table_meta.table_version = 1;
                    //Generate tags
                    a.table_meta.platform_tags = null;
                    List<string> tags = new List<string>();

                    //Replace null values
                    var badCompat = new PebbleApp_Compatibility_Hardware
                    {
                        firmware = null,
                        supported = false
                    };
                    if (a.compatibility.aplite == null)
                        a.compatibility.aplite = badCompat;
                    if (a.compatibility.basalt == null)
                        a.compatibility.basalt = badCompat;
                    if (a.compatibility.chalk == null)
                        a.compatibility.chalk = badCompat;
                    if (a.compatibility.diorite == null)
                        a.compatibility.diorite = badCompat;
                    if (a.compatibility.emery == null)
                        a.compatibility.emery = badCompat;

                    if (a.compatibility.aplite.supported)
                        tags.Add("aplite");
                    if (a.compatibility.basalt.supported)
                        tags.Add("basalt");
                    if (a.compatibility.chalk.supported)
                        tags.Add("chalk");
                    if (a.compatibility.diorite.supported)
                        tags.Add("diorite");
                    if (a.compatibility.emery.supported)
                        tags.Add("emery");

                    if (a.compatibility.android.supported)
                        tags.Add("android");
                    if (a.compatibility.ios.supported)
                        tags.Add("ios");

                    a.table_meta.platform_tags = tags.ToArray();

                    if (a.isPublished == 1 && a.uuid.Length > 0)
                    {
                        //Fix UUID
                        while (used_uuids.Contains(a.uuid))
                        {
                            char[] cs = a.uuid.ToCharArray();
                            char[] replace = "1234567890".ToCharArray();
                            int rr = rand.Next(0, 9);
                            if (cs[2] == replace[rr])
                                cs[2] = replace[rr + 1];
                            else
                                cs[2] = replace[rr];
                            a.uuid = new string(cs);
                        }

                        //If this is Timeline patched, change the URL.
                        if(Services.Locker.LockerService.CheckIfAppIsPbwPatched(a.id))
                        {
                            //This is a Timeline patched PBW. Remap the URLs.
                            string remap_url = "https://tl-pbws.get-rpws.com/"+a.id+".pbw";
                            a.latest_release.pbw_file = remap_url;
                            patchedCount++;
                        }

                        //Fetch metadata.
                        try
                        {
                            MemoryStream ms = new MemoryStream();

                            if (a.latest_release.pbw_file.StartsWith("https://tl-pbws.get-rpws.com/"))
                            {
                                //Open from file to save bandwidth (and $)
                                using (FileStream fs = new FileStream(@"C:\Users\Roman\tl_pbws\public\" + a.id + ".pbw", FileMode.Open))
                                    fs.CopyTo(ms);
                            }
                            else
                            {
                                ms = LibRpws2.LibRpws2Tools.DownloadFile(a.latest_release.pbw_file);
                            }
                            //Rewind.
                            ms.Position = 0;

                            //Open the data.
                            PbwInfo pbwinfo = ReleaseCreator.ReadPbwMetadata(ms, a.uuid, out string new_uuid);

                            //NOT DONE YET
                            a.table_meta.pbw_info = pbwinfo;

                            //Add
                            lock (appsToAdd)
                                appsToAdd.Add(a);
                            ms.Close();

                        }
                        catch (InvalidDataException ide)
                        {
                            Console.Write("Failed to fetch metadata for app " + a.id + " (" + a.title + "). This app won't be added. - Corrupted PBW ZIP\n");
                        }
                        catch (ReleaseValidationError ex)
                        {
                            Console.Write("Failed to fetch metadata for app " + a.id + " (" + a.title + "). This app won't be added. - Could not validate PBW; " + ex.message + "\n");
                        }
                        catch (Exception ex)
                        {
                            Console.Write("Failed to fetch metadata for app " + a.id + " (" + a.title + "). This app won't be added. - "+ex.Message+" - "+ex.StackTrace+" (Press ENTER) \n");
                            

                        }


                    }
                    else
                    {

                    }
                });

            }
            Console.WriteLine("\nSaving database...");
            //Add each
            foreach(var a in appsToAdd)
            {
                transact.ObjectInsert<AppstoreApp>(AppstoreApi.APP_TABLE, new DBreeze.Objects.DBreezeObject<AppstoreApp>
                {
                    NewEntity = true,
                    Entity = a,
                    Indexes = new List<DBreeze.Objects.DBreezeIndex>
                    {
                        new DBreeze.Objects.DBreezeIndex(1, a.id) { PrimaryIndex = true},
                    }
                });

                transact.ObjectInsert<string>(AppstoreApi.APP_UUID_MAP_TABLE, new DBreeze.Objects.DBreezeObject<string>
                {
                    NewEntity = true,
                    Entity = a.id,
                    Indexes = new List<DBreeze.Objects.DBreezeIndex>
                    {
                        new DBreeze.Objects.DBreezeIndex(1, a.uuid) { PrimaryIndex = true},
                    }
                });

                //Add likes
                AppstoreVoteApi.AddAppEntry(a.id, a.hearts);
            }
            transact.Commit();
            transact.Dispose();
        }

        private static PebbleApiLocalCachePage RunAppHttpApi(JsonSerializerSettings sett, string url = "https://pebble-appstore.romanport.com/api/raw/stream/index_dict.php?offset=0")
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                string app = reader.ReadToEnd();
                int preLen = app.Length;
                app = app.Replace(@"%rootUrl%\/files", "https://assets-static-1.romanport.com/pebble_appstore_static_originals").Replace("%rootUrl%files", "https://assets-static-1.romanport.com/pebble_appstore_static_originals");
                if (app.Contains("%rootUrl%"))
                    throw new Exception("VERY OLD ROOT URL DETECTED! "+app.Substring(app.IndexOf("%rootUrl%"), 30));
                return JsonConvert.DeserializeObject<PebbleApiLocalCachePage>(app, sett);
            }
        }

        private class PebbleApiLocalCachePage
        {
            public AppstoreApp[] data;
            public string next;
            public int total;
        }
    }
}
