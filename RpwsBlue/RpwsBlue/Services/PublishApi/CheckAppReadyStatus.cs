using LibRpws2.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services.PublishApi
{
    public static class CheckAppReadyStatus
    {
        public static AppReadyStatus CheckApp(PebbleAppDbStorage app)
        {
            var papp = app.app;
            AppReadyStatus status = new AppReadyStatus();

            //Validate each part of app.
            if(papp.latest_release == null)
                status.AddProblem("No releases! Add one in the \"Releases\" box.", AppReadyProblemIds.MissingRelease);

            try
            {
                papp.screenshot_images.GetItem();
            }
            catch
            {
                status.AddProblem("No screenshot images! Add some in the \"Assets\" box.", AppReadyProblemIds.MissingScreenshot);
            }

            try
            {
                papp.list_image.GetItem();
            }
            catch
            {
                status.AddProblem("No list image! Add one in the \"Assets\" box.", AppReadyProblemIds.MissingListImage);
            }

            try
            {
                papp.icon_image.GetItem();
            }
            catch
            {
                status.AddProblem("No icon image! Add one in the \"Assets\" box.", AppReadyProblemIds.MissingIconImage);
            }

            if (papp.uuid == null)
                status.AddProblem("No UUID! Add a release to obtain one.", AppReadyProblemIds.MissingUUID);
            else
            {
                if (papp.uuid.Length < 3)
                {
                    status.AddProblem("No UUID! Add a release to obtain one.", AppReadyProblemIds.MissingUUID);
                }
            }

            if (papp.description == null)
                status.AddProblem("App description is missing or too short.", AppReadyProblemIds.DescriptionTooShort);
            else
            {
                if(papp.description.Length < 10)
                    status.AddProblem("App description is missing or too short.", AppReadyProblemIds.DescriptionTooShort);
            }

            //Set some data in the returned value.
            status.is_published = app.dev.is_published;
            if (app.dbVersion >= 6)
            {
                if(app.dev.last_release_time != -1)
                    status.last_published = new DateTime(app.dev.last_release_time);
                else
                    status.last_published = null;
            }
            else
                status.last_published = null;

            if (app.dbVersion >= 6)
                status.changes_since_last_published = app.dev.changes_since_last_published;
            else
                status.changes_since_last_published = 0;

            return status;
        }
    }

    public class AppReadyStatus
    {
        public bool is_published;
        public bool can_be_published = true;
        public List<AppReadyProblems> problems = new List<AppReadyProblems>();

        public DateTime? last_published;
        public int changes_since_last_published;

        public void AddProblem(string message, AppReadyProblemIds id)
        {
            can_be_published = false;
            AppReadyProblems p = new AppReadyProblems
            {
                message = message,
                code = id
            };
            problems.Add(p);
        }
    }

    public class AppReadyProblems
    {
        public AppReadyProblemIds code;
        public string message;
    }

    public enum AppReadyProblemIds
    {
        MissingRelease,
        MissingScreenshot,
        MissingListImage,
        MissingIconImage,
        MissingUUID,
        DescriptionTooShort
    }
}
