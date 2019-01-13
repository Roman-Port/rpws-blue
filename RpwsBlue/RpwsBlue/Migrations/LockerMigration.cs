using LibRpws2.Entities;
using LibRpwsDatabase.Entities;
using LiteDB;
using RpwsBlue.Services.Locker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RpwsBlue.Migrations
{
    class LockerMigration
    {
        public static void MigrateLockerApps()
        {
            LiteDatabase src = LibRpws.LibRpwsCore.lite_database;
            LiteCollection<E_RPWS_User> src_users = src.GetCollection<E_RPWS_User>("accounts");
            LiteCollection<E_RPWS_AppToken> src_app_tokens = src.GetCollection<E_RPWS_AppToken>("app_tokens");
            E_RPWS_User[] users = src_users.FindAll().ToArray();
            int appTokensTotal = src_app_tokens.Count();
            Console.Write("\n");

            List<string> errors = new List<string>();

            //Loop
            int appTokensProcessed = 0;
            int appTokensInstalled = 0;
            int appTokensOriginal = 0;
            for (int i = 0; i < users.Length; i++)
            {
                E_RPWS_User user = users[i];
                Console.Write($"\rMigration {(int)(((float)i / (float)users.Length) * 100)}% complete... - User [{i} / {users.Length}] ({user.uuid}) - App Token [{appTokensProcessed} / {appTokensTotal}] - AppTokensInstalled={appTokensInstalled}, AppTokensOriginal={appTokensOriginal}");

                //Find all tokens.
                E_RPWS_AppToken[] tokens = src_app_tokens.Find(x => x.accountUuid == user.uuid).ToArray();
                foreach (E_RPWS_AppToken t in tokens)
                {
                    //Convert to a new object.
                    if (AppstoreApi.GetAppById(t.appId) == null)
                    {
                        errors.Add($"\rFailed to convert app {t.appId} because it did not exist in our database. Skipping...\n");
                        continue;
                    }
                    bool installed = user.lockerInstalled.Contains(t.appId);
                    E_LockerTokenV2 newToken = new E_LockerTokenV2
                    {
                        app_id = t.appId,
                        app_uuid = AppstoreApi.GetAppById(t.appId).uuid,
                        installed = installed,
                        token = t.token,
                        user_uuid = user.uuid,
                        is_original_pebble = t.isOriginalPebble,
                        creation_date = t.creationDate
                    };
                    LockerTool.GetCollection().Insert(newToken);

                    if (installed)
                        appTokensInstalled++;
                    if (t.isOriginalPebble)
                        appTokensOriginal++;
                    appTokensProcessed++;
                }
            }
            foreach (string s in errors)
                Console.WriteLine(s);
        }
    }
}
