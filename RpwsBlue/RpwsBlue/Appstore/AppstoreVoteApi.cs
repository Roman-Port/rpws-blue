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
using RpwsBlue.Entities;
using LiteDB;
using LibRpws2.ApiEntities;

namespace RpwsBlue
{
    public static class AppstoreVoteApi
    {
        /// <summary>
        /// Locked while editing the database.
        /// </summary>
        static LockClass dbLock = new LockClass();

        static CacheBlock<AppstoreAppVote> cache = new CacheBlock<AppstoreAppVote>(1000);
        
        /// <summary>
        /// Returns the LiteDB collection where all the content is stored.
        /// </summary>
        public static LiteCollection<AppstoreAppVote> GetCollection()
        {
            return LibRpws.LibRpwsCore.lite_database.GetCollection<AppstoreAppVote>("v3_hearts");
        }

        private static AppstoreAppVote PrivateGetAppEntry(string appId)
        {
            //Check if cached
            AppstoreAppVote a;
            if (cache.TryGetItem(appId, out a))
                return a;

            //Find app
            a = GetCollection().FindOne(x => x.appId == appId);
            if(a == null)
            {
                //App did not exist. Maybe this is an app that was published. Add entry.
                a = AddAppEntry(appId, 0);
            }

            //Add to cache
            cache.AddItem(a, appId);
            return a;
        }

        private static void PrivateSaveVoteEntry(AppstoreAppVote v)
        {
            GetCollection().Update(v);
        }

        private static void PrivateUpdateVoteOnAlgolia(AppstoreAppVote v)
        {
            AlgoliaApi.UpdateAppHearts(v.appId, v.total);
        }

        private static void PrivateRecalculateVotes(ref AppstoreAppVote v)
        {
            //Loop through each user and total the votes.
            int total = 0;
            foreach (var u in v.users.Values)
                total += u;

            //Set
            v.total = total;
        }

        /// <summary>
        /// Dangerous function to run that deletes apps with no votes.
        /// </summary>
        public static void Cleanup()
        {
            foreach(var e in GetCollection().FindAll())
            {
                if(e.total == 0)
                {
                    GetCollection().Delete(e._id);
                }
            }
        }

        /// <summary>
        /// Adds a new app.
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="initialVoteCount"></param>
        public static AppstoreAppVote AddAppEntry(string appId, int initialVoteCount)
        {
            //Create object
            AppstoreAppVote v = new AppstoreAppVote
            {
                appId = appId,
                total = initialVoteCount,
                users = new Dictionary<string, int>()
            };

            //If the initial vote count is not zero, add a user.
            if (initialVoteCount != 0)
                v.users.Add("anonymous", initialVoteCount);

            //Save
            v._id = GetCollection().Insert(v);
            return v;
        }

        /// <summary>
        /// Get the number of votes a specific user has placed.
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static int GetUserVote(string appId, string userId)
        {
            //Find the app.
            AppstoreAppVote v = PrivateGetAppEntry(appId);

            //Check if this user has voted on the app.
            if (!v.users.ContainsKey(userId))
                return 0;

            //The user has voted. Return it.
            return v.users[userId];
        }

        /// <summary>
        /// Get the total number of votes an app has.
        /// </summary>
        /// <param name="appId"></param>
        /// <returns></returns>
        public static int GetAppTotalVotes(string appId)
        {
            //Get app
            AppstoreAppVote v = PrivateGetAppEntry(appId);

            return v.total;
        }

        /// <summary>
        /// Vote on an app with a user.
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="userId"></param>
        /// <param name="vote"></param>
        public static AppstoreAppVote VoteOnApp(string appId, string userId, int vote)
        {
            //Lock the database from writes while processing this to prevent double votes. 
            AppstoreAppVote v;
            lock (dbLock)
            {
                //Get app
                v = PrivateGetAppEntry(appId);

                //See if this user has an entry inside of it.
                if(!v.users.ContainsKey(userId))
                {
                    //Add new user
                    v.users.Add(userId, vote);
                } else
                {
                    //Edit exsting vote
                    v.users[userId] = vote;
                }

                //Recalculate votes
                PrivateRecalculateVotes(ref v);

                //Save
                PrivateSaveVoteEntry(v);

                //Update cache
                cache.UpdateItem(v.appId, v);

                //Insert vote on Algolia
                PrivateUpdateVoteOnAlgolia(v);
            }
            return v;
        }

        /// <summary>
        /// Find multiple totals at once, speeding things up a ton.
        /// </summary>
        /// <param name="ids"></param>
        public static int[] GetTotals(string[] ids)
        {
            int[] totals = new int[ids.Length];

            //The cache will only aid us if we have everything cached. Try to get all items.
            bool cacheOk = true;
            for(int i = 0; i<ids.Length; i++)
            {
                if(cache.TryGetItem(ids[i], out AppstoreAppVote item))
                {
                    //Add to totals
                    totals[i] = item.total;
                } else
                {
                    //We're missing an item. Get all from the database.
                    cacheOk = false;
                    break;
                }
            }
            if (cacheOk)
                return totals;

            //First, find results.
            var collec = GetCollection();
            var results = collec.Find(x => ids.Contains(x.appId)).ToArray();

            //Now, sort the results.
            for(int i = 0; i<ids.Length; i++)
            {
                totals[i] = 0;
                foreach (var r in results)
                {
                    if (r.appId == ids[i]) {
                        totals[i] = r.total;
                        cache.AddItem(r, r.appId);
                        break;
                    }
                }
            }

            return totals;
        }
    }
}
