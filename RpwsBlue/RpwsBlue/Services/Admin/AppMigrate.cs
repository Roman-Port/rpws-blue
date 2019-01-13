using LibRpws;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LibRpws2.Entities;

namespace RpwsBlue.Services.Admin
{
    class AppMigrate
    {
        public static string OnReq(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            //Migrate the app to this. First, get it's data.
            var app = AppstoreApi.GetAppById(e.Request.Form["app"]);
            if(app == null)
            {
                return "The app '"+e.Request.Form["app"]+"' didn't exist.";
            }
            //Get the apps database.
            var collection = LibRpwsCore.lite_database.GetCollection<PebbleAppDbStorage>("edited_apps");
            //Check if it has already been migrated.
            if(collection.Find( x => x.app.id == app.id).Count() != 0)
            {
                return "The app '" + app.title + "' has already been migrated.";
            }
            //Make modifications to the app.
            app.isOriginal = 0;
            app.isPublished = 0;
            app.isTimelineKnownUnsupported = 0;
            app.author = ee.user.appDevName;
            app.developer_id = ee.user.pebbleId;
            //Migrate it.
            PebbleAppDbStorage db = new PebbleAppDbStorage
            {
                app = app,
                dbVersion = 2,
                deleted = false,
                dev = new PebbleApp_RPWS_Dev
                {
                    changes_pending = true,
                    release_history = new PebbleApp_Release[app.changelog.Length],
                }
            };
            //Add the release history objects.
            for(int i = 0; i<app.changelog.Length; i+=1)
            {
                var changelog = app.changelog[i];
                var latestRelease = app.latest_release;
                PebbleApp_Release release = new PebbleApp_Release();
                release.id = "AAAAAAAAAAAAAAAAAAAAAAAA"; //Placeholder because this is unknown.
                release.js_version = latestRelease.js_version;
                release.pbw_file = latestRelease.pbw_file;
                release.published_date = changelog.published_date;
                release.release_notes = changelog.release_notes;
                release.version = changelog.version;
                db.dev.release_history[i] = release;
            }
            //Insert into the database.
            collection.Insert(db);
            //Good to go.
            return "Migrated '" + app.title + "'. Please publish the app from inside the console.";
        }
    }
}
