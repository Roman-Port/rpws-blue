using Algolia.Search;
using LibRpws2.Entities;
using Newtonsoft.Json.Linq;
using RpwsBlue.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue
{
    public static class AlgoliaApi
    {
        public static Index GetIndex()
        {
            return Program.algolia.InitIndex("rpws-appstore-production");
        }

        private static AlgoliaAppstoreApp PrivateConvertApp(AppstoreApp a)
        {
            int hearts = AppstoreVoteApi.GetAppTotalVotes(a.id);
            return new AlgoliaAppstoreApp(a, hearts, hearts);
        }

        public static void AddApp(AppstoreApp a)
        {
            //Convert app
            AlgoliaAppstoreApp converted = PrivateConvertApp(a);

            Console.WriteLine($"Adding app \"{a.title}\" ({a.id}) to Algolia...");

            //Insert app
            GetIndex().AddObject(converted);
        }

        public static void UpdateApp(AppstoreApp a)
        {
            //Convert app
            AlgoliaAppstoreApp converted = PrivateConvertApp(a);

            Console.WriteLine($"Updating app \"{a.title}\" ({a.id}) to Algolia...");

            //Insert app
            GetIndex().SaveObject((JObject)JToken.FromObject(converted));
        }

        public static bool CheckIfAppExists(string id)
        {
            try
            {
                return GetIndex().GetObject(id) != null;
            } catch
            {
                return false;
            }
        }

        public static void AddOrUpdateApp(AppstoreApp a)
        {
            if (!CheckIfAppExists(a.id))
                AddApp(a);
            else
                UpdateApp(a);
        }

        public static void UpdateAppHearts(string id, int count)
        {
            GetIndex().PartialUpdateObject(new JObject
            {
                { "hearts", count },
                { "objectID", id }
            }, false);
        }

        public static void DeleteApp(string id)
        {
            GetIndex().DeleteObject(id);
        }
    }
}
