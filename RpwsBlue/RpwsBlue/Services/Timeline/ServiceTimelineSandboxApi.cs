using LibRpws;
using LibRpws.Timeline;
using LibRpwsDatabase.Entities;
using Newtonsoft.Json;
using RpwsBlue.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace RpwsBlue.Services.Timeline
{
    public static class ServiceTimelineSandboxApi
    {
        public static void OnGenerateSandboxRequest(Microsoft.AspNetCore.Http.HttpContext context, HttpSession ee)
        {
            //First, extract the UUID from the url.
            if (!ee.GET.ContainsKey("uuid"))
                throw new Exception("Missing UUID in url.");
            //Get the collection.
            var collection = ServiceTimeline.GetSandboxTokenCollection();
            //Generate a unique token.
            string token = GenerateToken();
            while (collection.Find(x => x.token == token).Count() != 0)
                token = GenerateToken();
            //Now, create the object to insert into the database.
            E_RPWS_TimelineSandboxToken sandbox = new E_RPWS_TimelineSandboxToken
            {
                appUuid = ee.GET["uuid"],
                creationDate = DateTime.UtcNow.Ticks,
                token = token,
                userUuid = ee.user.uuid
            };
            //Insert into the database.
            collection.Insert(sandbox);
            //Generate a reply.
            ApiSandboxReply reply = new ApiSandboxReply
            {
                token = sandbox.token,
                uuid = sandbox.appUuid
            };
            Program.QuickWriteJsonToDoc(context, reply);
        }

        private static string GenerateToken()
        {
            return LibRpwsCore.GenerateStringInFormat(@"&&&&&&&&-&&&&-&&&&-&&&&-&&&&&&&&&&&&");
        }
    }
}
