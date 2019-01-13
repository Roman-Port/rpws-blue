using LibRpws2.ApiEntities;
using LibRpws2.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpws2.Api
{
    public static class RpwsAppstore
    {
        private static CacheBlock<AppstoreApp> id_cache = new CacheBlock<AppstoreApp>(200);
        private static CacheBlock<AppstoreApp> uuid_cache = new CacheBlock<AppstoreApp>(200);

        public static AppstoreApp GetAppById(string id)
        {
            return LibRpws2Core.client.SendQuestionGetReplyWait<AppstoreApp>(id, RpwsNetQuestion.GetAppById);
        }

        public static AppstoreApp GetAppByUUID(string id)
        {
            return LibRpws2Core.client.SendQuestionGetReplyWait<AppstoreApp>(id, RpwsNetQuestion.GetAppByUUID);
        }

        public static AppstoreApp[] GetAppsByIdIndexed(string id)
        {
            return LibRpws2Core.client.SendQuestionGetReplyWait<AppstoreApp[]>(id, RpwsNetQuestion.GetAppsByIdIndexed);
        }

        public static AppstoreApp[] GetAppsByUUIDIndexed(string id)
        {
            return LibRpws2Core.client.SendQuestionGetReplyWait<AppstoreApp[]>(id, RpwsNetQuestion.GetAppsByUUIDIndexed);
        }
    }
}
