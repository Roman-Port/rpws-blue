using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LibRpws;
using LibRpws2.QuestionArgs;
using LibRpwsDatabase.Entities;
using RpwsServerBridge.Entities;
using LibRpws2.Entities;

namespace RpwsBlue.MasterServer.Questions
{
    class MasterServerAppstoreApi
    {
        public static void OnEvent_GetAppById(string data, NetworkPacket p)
        {
            //Get
            AppstoreApp reply = AppstoreApi.GetAppById(data);
            //Write
            p.ReplyToQuestion(reply);
        }

        public static void OnEvent_GetAppByUuid(string data, NetworkPacket p)
        {
            //Get
            AppstoreApp reply = AppstoreApi.GetAppByUUID(data);
            //Write
            p.ReplyToQuestion(reply);
        }

        public static void OnEvent_GetAppsByIdIndex(string[] data, NetworkPacket p)
        {
            //Get
            AppstoreApp[] reply = AppstoreApi.GetAppsById(data);
            //Write
            p.ReplyToQuestion(reply);
        }

        public static void OnEvent_GetAppsByUuidIndex(string[] data, NetworkPacket p)
        {
            //Get
            AppstoreApp[] reply = AppstoreApi.GetAppsByUUID(data);
            //Write
            p.ReplyToQuestion(reply);
        }
    }
}
