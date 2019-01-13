using LibRpws;
using LibRpws2.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services.PublishApi.PublishServices
{
    public static class PublishChanges
    {
        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee, PebbleAppDbStorage app)
        {
            PublishChangesStatus status = PublishApp(app);
            Program.QuickWriteJsonToDoc(e, status);
        }

        public static PublishChangesStatus PublishApp(PebbleAppDbStorage app)
        {
            //Validate app
            AppReadyStatus validationStatus = CheckAppReadyStatus.CheckApp(app);
            if (!validationStatus.can_be_published)
            {
                //Cannot be published!
                return new PublishChangesStatus
                {
                    ok = false,
                    message = $"Cannot publish - There are {validationStatus.problems.Count} problems that must be fixed."
                };
            }

            //Publish changes
            string oldUuid = null;
            if(app.app.uuid != null)
            {
                if (app.app.uuid.Length > 1)
                    oldUuid = app.app.uuid;
            }
            try
            {
                AppstoreApi.UpdateAppById(app.app, oldUuid);
            } catch (Exception ex)
            {
                //Cannot be published!
                return new PublishChangesStatus
                {
                    ok = false,
                    message = $"Failed to publish - An unknown error '{ex.Message}' occurred when trying to publish this application."
                };
            }

            //Publish to Algolia
            try
            {
                AlgoliaApi.AddOrUpdateApp(app.app);
            } catch (Exception ex)
            {
                //Failed!
                Console.WriteLine($"Algolia updating error: {ex.Message} {ex.StackTrace}");
                return new PublishChangesStatus
                {
                    ok = false,
                    message = $"Failed to publish changes to Algolia. Please try again later. Details: {ex.Message}"
                };
            }

            //Set some data on the app.
            app.dev.changes_since_last_published = -1; //This will equal 0 after saving.
            app.dev.changes_pending = false;
            app.dev.is_published = true;
            app.dev.last_release_time = DateTime.UtcNow.Ticks;
            CorePublishApi.SaveApp(app);

            //Return true
            return new PublishChangesStatus
            {
                ok = true,
                message = "App was successfully updated!"
            };
        }
    }

    public class PublishChangesStatus
    {
        public bool ok;
        public string message;
    }
}
