using LibRpws;
using LibRpws2.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services.PublishApi.PublishServices
{
    public static class DeleteApp
    {
        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee, PebbleAppDbStorage app)
        {
            Program.QuickWriteJsonToDoc(e, DeleteAppRequest(app));
        }

        static EditCompanionsReply DeleteAppRequest(PebbleAppDbStorage app)
        {
            //First, remove the app from Algolia.
            try
            {
                AlgoliaApi.DeleteApp(app.app.id);
            } catch
            {
                //Failed to remove from Algolia.
                return new EditCompanionsReply
                {
                    message = "Failed to remove from Algolia. Is Algolia down?",
                    ok = false
                };
            }

            //Delete from apps database.
            try
            {
                AppstoreApi.DeleteApp(app.app);
            } catch
            {
                return new EditCompanionsReply
                {
                    message = "Failed to remove from DBreeze Appstore database.",
                    ok = false
                };
            }

            //Delete from app list
            try
            {
                CorePublishApi.DeleteApp(app);
            }
            catch
            {
                return new EditCompanionsReply
                {
                    message = "Failed to remove from LiteDB user database.",
                    ok = false
                };
            }

            //OK
            return new EditCompanionsReply
            {
                message = $"Deleted app {app.app.title}.",
                ok = true
            };
        }
    }
}
