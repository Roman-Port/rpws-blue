using LibRpws;
using LibRpwsDatabase.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RpwsBlue.Services.Timeline
{
    public static class ServiceTimelineApi
    {
        public static void OnPinRequest(Microsoft.AspNetCore.Http.HttpContext context, HttpSession ee)
        {
            //Get the ID of the pin.
            if(context.Request.Path.ToString().Length <= "/v1/user/pins/".Length)
            {
                //No pin name.
                Program.QuickWriteToDoc(context, "Not Found", "text/html", 404);
                return;
            }
            string pinId = context.Request.Path.ToString().Substring("/v1/user/pins/".Length).TrimEnd('/');
            string method = context.Request.Method.ToUpper();
            //Authorize the user.
            string token = "";
            if (context.Request.Headers.ContainsKey("X-User-Token"))
                token = context.Request.Headers["X-User-Token"];
            
            if(LibRpws.AppTokens.AppTokens.ValidateToken(token, out string appId, out E_RPWS_User user) == false)
            {
                Program.QuickWriteToDoc(context, "Not Authorized", "text/html", 401);
                return;
            }


            switch(method)
            {
                case "GET":
                    OnPinRequestGet(context, ee, pinId);
                    break;
                case "DELETE":
                    OnPinRequestDelete(context, ee, pinId);
                    break;
                case "PUT":
                    //Put pin
                    OnPinRequestPut(context, ee, pinId, appId,user);
                    break;
            }
            
            
        }

        

        private static void OnPinRequestGet(Microsoft.AspNetCore.Http.HttpContext context, HttpSession ee, string pinUrlId)
        {
            //Get the collection.
            var collection = LibRpwsCore.lite_database.GetCollection<E_RPWS_TimelinePinContainer>("timeline_pins");

            //Search for this pin ID
            var pins = collection.Find(x => x.userId == ee.user.uuid && pinUrlId == x.urlId).ToArray();
        }

        private static void OnPinRequestDelete(Microsoft.AspNetCore.Http.HttpContext context, HttpSession ee, string pinUrlId)
        {
            //Get the collection.
            var collection = LibRpwsCore.lite_database.GetCollection<E_RPWS_TimelinePinContainer>("timeline_pins");

            //Delete pins with ID
            collection.Delete(x => x.userId == ee.user.uuid && pinUrlId == x.urlId);

            //Add the event for the deletion of this pin.
            //todo

            //Respond with the okay.
            Program.QuickWriteToDoc(context, "OK", "text/plain", 200);
        }

        private static void OnPinRequestPut(Microsoft.AspNetCore.Http.HttpContext context, HttpSession ee, string pinUrlId, string appId, E_RPWS_User user)
        {
            //Deserialize the pin.
            byte[] pinBuf = new byte[(int)context.Request.ContentLength];
            context.Request.Body.Read(pinBuf, 0, pinBuf.Length);
            string body = Encoding.UTF8.GetString(pinBuf);
            E_RPWS_TimelinePin pin = JsonConvert.DeserializeObject<E_RPWS_TimelinePin>(body);
            //Add the metadata
            pin.source = "web";
            pin.topicKeys = new string[] { };
            pin.guid = LibRpwsCore.GenerateStringInFormat("&&&&&&&&-&&&&-5b77-&&&&-&&&&&&&&&&&&");
            pin.dataSource = "uuid:" + AppstoreApi.GetAppById(appId).uuid;
            //For now, set the create time and update time to now. I'll probably have to fix that later.
            pin.createTime = DateTime.UtcNow;
            pin.updateTime = pin.createTime;
            //Create the object
            E_RPWS_TimelinePinContainer c = new E_RPWS_TimelinePinContainer();
            c.internalUuid = DateTime.UtcNow.Ticks.ToString() + LibRpwsCore.GenerateRandomString(16);
            c.pin = pin;
            c.appId = appId;
            c.urlId = pinUrlId;
            c.userId = user.uuid;
            c.internalAppVersion = 3;
            //Get the collection.
            var collection = LibRpwsCore.lite_database.GetCollection<E_RPWS_TimelinePinContainer>("timeline_pins");
            //Remove existing pins 
            var existingPins = collection.Find(x => x.appId == appId && x.urlId == pinUrlId && x.userId == user.uuid).ToArray();
            foreach(var p in existingPins)
            {
                //TOdo: Remove
            }
            
            
            //Insert pin
            collection.Insert(c);
            //Create the event.
            E_RPWS_TimelineEventAction action = E_RPWS_TimelineEventAction.timeline_pin_create;
            /*if (existingPins.Length > 0)
                action = E_RPWS_TimelineEventAction.timeline_pin_update;*/
            LibRpws.Timeline.ServiceTimeline.AddEvent(action, c.internalUuid, ee, user);
            //Create some sort of reply.
            Program.QuickWriteToDoc(context, "OK", "text/plain", 200);
        }
    }
}
