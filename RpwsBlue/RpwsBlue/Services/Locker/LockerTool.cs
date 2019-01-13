using LibRpws2.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RpwsBlue.Services.Locker
{
    public static class LockerTool
    {
        public const int TOKEN_STRING_LENGTH = 26;

        public static LiteDB.LiteCollection<E_LockerTokenV2> GetCollection()
        {
            return LibRpws.LibRpwsCore.lite_database.GetCollection<E_LockerTokenV2>("locker_tokens");
        }

        private static string GenerateTokenString()
        {
            string token = LibRpws.LibRpwsCore.GenerateRandomString(TOKEN_STRING_LENGTH);
            var collection = GetCollection();
            while (collection.Count(x => x.token == token) != 0)
                token = LibRpws.LibRpwsCore.GenerateRandomString(TOKEN_STRING_LENGTH);
            return token;
        }

        /// <summary>
        /// Get an appstore app token without checking if an app is installed. Create one if it doesn't exist.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="user_uuid"></param>
        /// <returns></returns>
        public static E_LockerTokenV2 GetAppToken(AppstoreApp app, string user_uuid)
        {
            var collection = GetCollection();
            E_LockerTokenV2[] tokens = collection.Find(x => x.app_id == app.id && x.app_uuid == app.uuid && x.user_uuid == user_uuid).ToArray();
            if (tokens.Length == 0)
            {
                //No token exists. Generate one.
                string token = GenerateTokenString();
                E_LockerTokenV2 t = new E_LockerTokenV2
                {
                    app_id = app.id,
                    app_uuid = app.uuid,
                    installed = false,
                    token = token,
                    user_uuid = user_uuid,
                    creation_date = DateTime.UtcNow.Ticks,
                    is_original_pebble = false
                };
                //Insert
                t._id = collection.Insert(t);

                //Return this token.
                return t;
            }
            return tokens[0];
        }

        /// <summary>
        /// Returns all apps a user has installed.
        /// </summary>
        /// <param name="user_uuid"></param>
        /// <returns></returns>
        public static E_LockerTokenV2[] GetInstalledApps(string user_uuid)
        {
            var collection = GetCollection();
            //Return tokens
            return collection.Find(x => x.user_uuid == user_uuid && x.installed == true).ToArray();
        }

        /// <summary>
        /// Returns true if the app ID is installed.
        /// </summary>
        /// <param name="user_uuid"></param>
        /// <param name="appId"></param>
        /// <returns></returns>
        public static bool GetIsInstalled(string user_uuid, string appId)
        {
            E_LockerTokenV2[] installed_apps = GetInstalledApps(user_uuid);
            return installed_apps.Where(x => x.app_id == appId && x.installed == true).Count() == 1;
        }

        public static E_LockerTokenV2 InstallApp(AppstoreApp app, string user_uuid)
        {
            //Obtain the token.
            E_LockerTokenV2 token = GetAppToken(app, user_uuid);
            token.installed = true;
            //Save
            var collection = GetCollection();
            collection.Update(token);
            //Return token
            return token;
        }

        public static E_LockerTokenV2 DeleteApp(AppstoreApp app, string user_uuid)
        {
            //Obtain the token.
            E_LockerTokenV2 token = GetAppToken(app, user_uuid);
            token.installed = false;
            //Save
            var collection = GetCollection();
            collection.Update(token);
            //Return token
            return token;
        }

        public static E_LockerTokenV2 UninstallApp(AppstoreApp app, string user_uuid)
        {
            //Obtain the token.
            E_LockerTokenV2 token = GetAppToken(app, user_uuid);
            token.installed = false;
            //Save
            var collection = GetCollection();
            collection.Update(token);
            //Return token
            return token;
        }

        public static LockerAppFormat ConvertToLockerFormat(AppstoreApp a, string user_uuid, LibRpws2.Analytics.RpwsAnalytics ra = null)
        {
            if (ra != null)
                ra.NextCheckpoint("Getting app token");
            //Obtain token.
            E_LockerTokenV2 token = LockerTool.GetAppToken(a, user_uuid);
            if (ra != null)
                ra.NextCheckpoint("Converting app");
            try
            {
                return new LockerAppFormat(a, token.token, AppstoreVoteApi.GetAppTotalVotes(a.id));
            }
            catch (Exception ex)
            {
                LibRpws.LibRpwsCore.Log($"Failed to convert locker app: {ex.Message} - {ex.StackTrace}", LibRpws.RpwsLogLevel.High);
                return null;
            }
        }
    }
}
