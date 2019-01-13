using LibRpws2.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Migrations.MasterMigrations
{
    /// <summary>
    /// Migration for January 7, 2019
    /// </summary>
    class _7Jan19
    {
        public static void RunMigration()
        {
            //Check to see if the migration should be done.
            var db = LibRpws.LibRpwsCore.lite_database;
            if(db.CollectionExists("locker_tokens"))
            {
                Console.WriteLine("CANCELING MIGRATION: locker_tokens collection already exists!");
                return;
            }
            if (db.CollectionExists("v3_hearts"))
            {
                Console.WriteLine("CANCELING MIGRATION: v3_hearts collection already exists!");
                return;
            }

            //Migrate locker tokens
            Console.WriteLine("[Migration] Migarting locker apps...");
            LockerMigration.MigrateLockerApps();

            //Nuke old tokens database.
            Console.WriteLine("[Migration] Dropping old locker token database...");
            db.DropCollection("app_tokens");

            //Migrate app hearts
            Console.WriteLine("[Migration] Migrating votes...\n");
            using(var t = AppstoreApi.GetTransaction())
            {
                var e = t.SelectForward<byte[], byte[]>(AppstoreApi.APP_TABLE);
                int index = 0;
                int totalHearts = 0;
                foreach(var ee in e)
                {
                    AppstoreApp app = ee.ObjectGet<AppstoreApp>().Entity;
                    Console.Write($"\rMigrating app {app.id} ({app.hearts}) - #{index} - Total: {totalHearts}");
                    AppstoreVoteApi.AddAppEntry(app.id, app.hearts);
                    totalHearts += app.hearts;
                    index++;
                }
            }

            //Done!
            Console.WriteLine("\nMigration done! Continuing RPWS startup...");
        }
    }
}
