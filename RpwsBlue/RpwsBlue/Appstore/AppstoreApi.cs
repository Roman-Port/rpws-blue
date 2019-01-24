using System;
using System.Collections.Generic;
using System.Text;
using RpwsServerBridge.Entities;
using LibRpws2.Entities;
using LibRpws2.Analytics;
using System.Linq;
using DBreeze;
using DBreeze.Transactions;
using DBreeze.Utils;

namespace RpwsBlue
{
    public static class AppstoreApi
    {
        public static DBreezeEngine engine;

        public const string APP_TABLE = "apps_v3";
        public const string APP_UUID_MAP_TABLE = "apps_v3_uuidmap";

        public static Transaction GetTransaction()
        {
            //Check if database isn't opened yet.
            if(engine == null)
            {
                engine = new DBreezeEngine(Program.config.appstore_database_location);
                DBreeze.Utils.CustomSerializator.ByteArraySerializator = (object o) => { return NetJSON.NetJSON.Serialize(o).To_UTF8Bytes(); };
                DBreeze.Utils.CustomSerializator.ByteArrayDeSerializator = (byte[] bt, Type t) => { return NetJSON.NetJSON.Deserialize(t, bt.UTF8_GetString()); };
            }
            return engine.GetTransaction();
        }

        private static string GetAppIdByUuid(string uuid)
        {
            //Lookup.
            var t = GetTransaction(); //Get the object to use
            var e = t.Select<byte[], byte[]>(APP_UUID_MAP_TABLE, 1.ToIndex(uuid)); //actually extract the index.

            string app = null;
            if (e.Exists)
                app = e.ObjectGet<string>().Entity;

            //Correctly close and return.
            t.Dispose();
            return app;
        }

        private static AppstoreApp GetAppByIndex(string data, byte index)
        {
            if (data == null)
                return null;

            var t = GetTransaction(); //Get the object to use
            var e = t.Select<byte[], byte[]>(APP_TABLE, index.ToIndex(data)); //actually extract the index.

            AppstoreApp app = null;
            if (e.Exists)
                app = e.ObjectGet<AppstoreApp>().Entity;

            //Correctly close and return.
            t.Dispose();
            return app;
        }

        private static AppstoreApp[] GetAppsByIndexArrayIndexed(string[] data, byte index)
        {
            AppstoreApp[] apps = new AppstoreApp[data.Length];
            for (int i = 0; i < apps.Length; i++)
                apps[i] = GetAppByIndex(data[i], index);
            return apps;
        }

        public static void DeleteApp(AppstoreApp app)
        {
            var t = GetTransaction();

            //Delete ID
            t.ObjectRemove(APP_TABLE, 1.ToIndex(app.id));

            //Delete from UUID map
            if(app.uuid != null)
            {
                if(app.uuid.Length > 3)
                {
                    try
                    {
                        t.ObjectRemove(APP_UUID_MAP_TABLE, 1.ToIndex(app.uuid));
                    } catch
                    {

                    }
                }
            }

            //Commit
            t.Commit();
        }

        /// <summary>
        /// Get an app by it's ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static AppstoreApp GetAppById(string id)
        {
            return GetAppByIndex(id, 1);
        }

        /// <summary>
        /// Get an app by it's UUID.
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
        public static AppstoreApp GetAppByUUID(string uuid)
        {
            string id = GetAppIdByUuid(uuid); //Lookup ID
            return GetAppByIndex(id, 1);
        }

        /// <summary>
        /// Returns an array of apps with the IDs matching the indexes of the output (so if there are apps not found, they will still exist in the array)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static AppstoreApp[] GetAppsById(string[] id)
        {
            return GetAppsByIndexArrayIndexed(id, 1);
        }

        /// <summary>
        /// Returns an array of apps with the UUIDs matching the indexes of the output (so if there are apps not found, they will still exist in the array)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static AppstoreApp[] GetAppsByUUID(string[] uuid)
        {
            //Lookup IDs
            string[] ids = new string[uuid.Length];
            for (int i = 0; i < uuid.Length; i++)
                ids[i] = GetAppIdByUuid(uuid[i]);

            return GetAppsByIndexArrayIndexed(uuid, 1);
        }

        /// <summary>
        /// Updates or adds an app the appstore.
        /// </summary>
        /// <param name="app">App to add</param>
        /// <param name="oldUuid">(Optional) If the UUID is not null, it will remap the old UUID to this app.</param>
        public static void UpdateAppById(AppstoreApp app, string oldUuid)
        {
            //Get some data
            bool isValidUuid = app.uuid != null;
            if (isValidUuid)
                isValidUuid = app.uuid.Length > 1;

            //Get a transaction.
            var t = GetTransaction();

            //Delete UUID from map
            if (oldUuid != null)
                t.RemoveKey(APP_UUID_MAP_TABLE, 1.ToIndex(oldUuid));

            //Delete the current app.
            t.RemoveKey(APP_TABLE, 1.ToIndex(app.id));

            //Insert the new app.
            t.ObjectInsert<AppstoreApp>(APP_TABLE, new DBreeze.Objects.DBreezeObject<AppstoreApp>
            {
                NewEntity = true,
                Entity = app,
                Indexes = new List<DBreeze.Objects.DBreezeIndex>
                {
                    new DBreeze.Objects.DBreezeIndex(1, app.id) { PrimaryIndex = true},
                }
            });

            //Insert the new UUID map
            if(isValidUuid)
            {
                t.ObjectInsert<string>(APP_UUID_MAP_TABLE, new DBreeze.Objects.DBreezeObject<string>
                {
                    NewEntity = true,
                    Entity = app.id,
                    Indexes = new List<DBreeze.Objects.DBreezeIndex>
                {
                    new DBreeze.Objects.DBreezeIndex(1, app.uuid) { PrimaryIndex = true},
                }
                });
            }

            //Commit
            t.Commit();
        }

        public static bool AddCommentToAppId(AppstoreComment comment, string appId)
        {
            //Grab the app
            AppstoreApp app = GetAppById(appId);

            if (app == null)
                return false;

            //Add comment
            if (app.comments == null)
                app.comments = new List<AppstoreComment>();
            app.comments.Add(comment);

            //Save
            UpdateAppById(app, null);
            return true;
        }
    }
}
