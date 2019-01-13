using LibRpws;
using LibRpws2.Entities;
using LibRpwsDatabase.Entities;
using Newtonsoft.Json;
using RpwsBlue.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RpwsBlue.Services.PublishApi
{
    public static class CorePublishApi
    {
        public static LiteDB.LiteCollection<PebbleAppDbStorage> collection;
        public const int CURRENT_DB_VERSION = 7;
        //4: Added the last edited date
        //5: Moved metadata to the front of the item.

        public static void Init()
        {
            //Get collection
            collection = LibRpwsCore.lite_database.GetCollection<PebbleAppDbStorage>("edited_apps");
        }

        public static void SaveApp(PebbleAppDbStorage app)
        {
            //Set vars.
            app.dev.last_edit_time = DateTime.UtcNow.Ticks;
            if (app.dbVersion < 6)
                app.dev.changes_since_last_published = 0;
            app.dev.changes_since_last_published++;
            app.dbVersion = CURRENT_DB_VERSION;

            //Save
            collection.Update(app);
        } 

        public static void DeleteApp(PebbleAppDbStorage app)
        {
            collection.Delete(app._id);
        }

        /// <summary>
        /// Sets the required request headers. If this is returned true, end the function.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool SetRequestHeaders(Microsoft.AspNetCore.Http.HttpContext e)
        {
            e.Response.Headers.Add("Access-Control-Allow-Origin", "https://publish.get-rpws.com");
            e.Response.Headers.Add("Access-Control-Allow-Credentials", "true");

            if (Program.FindRequestMethod(e) == RequestHttpMethod.options)
                return true;
            return false;
        }

        public static void OnCreateDevAccountRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            if (SetRequestHeaders(e))
                return;

            string error;
            //Check to see if the name they gave is okay.
            string name = e.Request.Query["name"];
            if (name.ToLower().Contains("changed later") || name.ToLower().Contains("between 4-20"))
            {
                error = "Oh, aren't you funny.";
            }
            else
            {
                //Check if it fits the criteria.
                if (name.Length >= 4 && name.Length <= 20)
                {
                    //Check if an app already exists with that name.
                    //I should probaby wait for the cache to finish, but oh well.
                    int matches = LibRpwsCore.lite_database.GetCollection<E_RPWS_User>("accounts").Find(x => x.appDevName == name).Count();
                    if (matches != 0)
                    {
                        //There is already a developer with this name.
                        error = "There seems to already be a developer with that name. Try something else.";
                    }
                    else
                    {
                        //Good to go.
                        ee.user.isAppDev = true;
                        ee.user.appDevName = name;
                        LibRpws.Users.LibRpwsUsers.UpdateUser(ee.user);
                        //Redirect to the applications page.
                        error = null;
                    }
                }
                else
                {
                    //Wrong.
                    error = "Make sure that name is between 4-20 characters.";
                }
            }

            //Respond.
            Program.QuickWriteJsonToDoc(e, new CreateDevAccountReply
            {
                error = error,
                ok = error == null
            });
        }

        class CreateDevAccountReply
        {
            public bool ok;
            public string error;
        }

        public static void OnLoginRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //Take in params from OAUTH.
            string grant_token = ee.GET["grant_token"];

            //Authenticate and obtain a token.
            var token = RpwsBlue.Services.OauthV2.OauthV2.InternalFinishGrant(grant_token);

            if(token.ok == false)
            {
                //Report failure.
                Program.QuickWriteToDoc(e, "<html><head><title>Oops...</title></head><body><h1>Sorry...</h1><p>Sorry, that grant token was invalid. Authentication failed or it expired. Please try again or <a href=\"https://get-rpws.com/support\" target=\"_blank\">contact support</a>. Thanks!</p></body></html>", "text/html", 500);
                return;
            }
            
            //Send a token.
            e.Response.Cookies.Append("access-token", token.access_token, new Microsoft.AspNetCore.Http.CookieOptions
            {
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None,
                IsEssential = true,
                Path = "/v1/publishing/"
            });

            //Redirect
            e.Response.Headers.Add("location", token.initial_args["publish_path"]);
            Program.QuickWriteToDoc(e, "hold on - you should be redirected to "+ token.initial_args["publish_path"], "text/plain", 302);
        }

        public static void OnLogoutRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //Accept headers
            if (SetRequestHeaders(e))
                return;

            //Delete token
            e.Response.Cookies.Append("access-token", "", new Microsoft.AspNetCore.Http.CookieOptions
            {
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None,
                IsEssential = true,
                Path = "/v1/publishing/"
            });

            //Respond with OK
            Program.QuickWriteToDoc(e, "{}", "application/json");
        }

        class ClaimDataReply
        {
            public AppstoreClaimRequest request;
            public Dictionary<string, AppstoreConvertedApp> apps;
            public DateTime time;
        }

        /// <summary>
        /// Called when a request is sent to the old publishing. Redirect.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="ee"></param>
        public static void OnOldPublishEndpoint(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            LibRpws.LibRpwsCore.Redirect(e, "https://publish.get-rpws.com/#legacy");
        }

        public static void OnClaimRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            if (SetRequestHeaders(e))
                return;

            //Get claim
            var claim = AppstoreClaimRequest.GetCollection().FindOne(x => x.uuid == ee.GET["id"] && x.userUuid == ee.user.uuid);
            if (claim == null)
                throw new RpwsStandardHttpException("That claim doesn't exist or you did not create it.");

            //Create time string
            string time = $"{DateTime.UtcNow.ToLongDateString()} at {DateTime.UtcNow.ToLongTimeString()} UTC";

            //If this is a get request, send the info. If it is a post request, send me and email.
            RequestHttpMethod method = Program.FindRequestMethod(e);
            if (method == RequestHttpMethod.get)
            {
                //Generate a reply
                ClaimDataReply r = new ClaimDataReply
                {
                    apps = new Dictionary<string, AppstoreConvertedApp>(),
                    request = claim,
                    time = new DateTime(claim.open_time)
                };
                foreach (var a in claim.appIds)
                {
                    if (!r.apps.ContainsKey(a))
                        r.apps.Add(a, new AppstoreConvertedApp(AppstoreApi.GetAppById(a), "basalt"));
                }
                Program.QuickWriteJsonToDoc(e, r);
            } else if (method == RequestHttpMethod.post)
            {
                //Get message
                string message = Program.GetPostString(e);

                //Send emails
                claim.SendClientEmail("You sent a message", $"Hi there, \n\nOn {time}, you sent the following message regarding your app claim. This is only an E-Mail log for you to keep.\n\n\"{message}\"");
                claim.SendWebmasterEmail("Client sent a message", $"Hi there, \n\nOn {time}, a client sent the following message. This is only an E-Mail log for you to keep.\n\n\"{message}\"");

                //Add to info
                if (claim.messageLog == null)
                    claim.messageLog = new List<AppstoreClaimRequestMessage>();

                claim.messageLog.Add(new AppstoreClaimRequestMessage
                {
                    contents = message,
                    fromClient = true,
                    time = DateTime.UtcNow.Ticks,
                    time_string = JsonConvert.SerializeObject(DateTime.UtcNow.Ticks).Trim('"')
                });

                //Save
                AppstoreClaimRequest.GetCollection().Update(claim);

                //Confirm
                Program.QuickWriteJsonToDoc(e, new Dictionary<string, bool>
                {
                    {"ok", true }
                });
            } else if (method == RequestHttpMethod.delete) {
                //Move ticket to archive.
                AppstoreClaimRequest.GetCollection().Delete(claim._id);
                claim._id = 0;
                AppstoreClaimRequest.GetArchiveCollection().Insert(claim);

                //Send an email
                claim.SendClientEmail("You closed this claim", $"Hi there,\n\nOn {time}, you closed this app claim. It can no longer be reopened. Thank you for using RPWS.");
                claim.SendWebmasterEmail("User closed this claim", $"On {time}, this claim was closed.");

                //Confirm.
                Program.QuickWriteJsonToDoc(e, new Dictionary<string, bool>
                {
                    {"ok", true }
                });
            } else
            {
                throw new RpwsStandardHttpException("Unknown method.");
            }
        }

        public static void OnAppRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            if (SetRequestHeaders(e))
                return;
            //Find the app in question.
            PebbleAppDbStorage app = collection.FindOne(x => x.app.id == ee.GET["id"] && x.app.developer_id == ee.user.pebbleId);
            if (app == null)
                throw new RpwsStandardHttpException("That app does not exist or you do not own it.");
            //Do migrations.
            //If the db version is 6 or lower, releases were stored as a dict. Fix.
            if(app.dbVersion <= 6 && app.dev.release_history.Length > 0)
            {
                //Grab the latest release from the release history instead.
                Console.WriteLine("Doing migration COREPUBLISHAPI.MIGRATE_LATEST_RELEASE...");
                PebbleApp_Release r = app.dev.release_history[app.dev.release_history.Length - 1];
                app.app.latest_release = r;
                SaveApp(app);
                Console.WriteLine("Finished migration.");
            }
            if(app.app.table_meta == null)
            {

            } else
            {
                if (app.app.table_meta.pbw_info == null && app.app.table_meta.appinfo != null)
                {
                    //We need to migrate.
                    //Get a release.
                    var release = app.app.latest_release;
                    if (release != null)
                    {
                        Console.WriteLine("Doing migration COREPUBLISHAPI.MIGRATE_TABLE_META>PBW_INFO...");

                        try
                        {
                            //First, download the release.
                            MemoryStream r = LibRpws2.LibRpws2Tools.DownloadFile(release.pbw_file);

                            //Rescan
                            app.app.table_meta.pbw_info = PublishServices.ReleaseCreator.ReadPbwMetadata(r, app.app.uuid, out string new_uuid);

                            //Wipe old data
                            app.app.table_meta.appinfo = null;

                            //Save
                            SaveApp(app);
                        }
                        catch
                        {
                            Console.WriteLine("Failed to complete migration for app. Ignoring and marking it as dirty.");
                            //Save it after disabling the migration. The user will need to recreate a release.
                            app.app.table_meta.appinfo = null;

                        }
                    }
                }
            }


            //Get the service
            string serviceName = ee.GET["service"];
            switch (serviceName)
            {
                case "info":
                    PublishServices.AppInfo.OnRequest(e, ee, app);
                    break;
                case "update_asset":
                    PublishServices.UpdateAssets.OnRequest(e, ee, app);
                    break;
                case "release_creator":
                    PublishServices.ReleaseCreator.OnRequest(e, ee, app);
                    break;
                case "update_appstore":
                    PublishServices.PublishChanges.OnRequest(e, ee, app);
                    break;
                case "update":
                    PublishServices.ListingChange.OnRequest(e, ee, app);
                    break;
                case "edit_companions":
                    PublishServices.EditCompanions.OnRequest(e, ee, app);
                    break;
                case "delete_app":
                    PublishServices.DeleteApp.OnRequest(e, ee, app);
                    break;
                default:
                    //Unknown
                    throw new RpwsStandardHttpException("Unknown service.");
            }
        }
    }
}
