using LibRpws;
using LibRpws2.Entities;
using RpwsBlue.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services.Appstore.AppServices
{
    public class AppComments
    {
        public static void OnAddComment(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee, AppstoreApp a)
        {
            //Stop if not authorized
            if(ee.user == null)
            {
                throw new RpwsStandardHttpException("Not Authenticated", false, 403);
            }
            
            //Parse body
            ApiAddCommentRequest payload = Program.GetPostBodyJson<ApiAddCommentRequest>(e);

            //Create comment
            AppstoreComment c = new AppstoreComment
            {
                content = payload.content,
                time = DateTime.UtcNow.Ticks,
                total_votes = 0,
                user_id = ee.user.pebbleId,
                user_uuid = ee.user.uuid,
                user_name = ee.user.name,
                user_profile_image = "",
                vote_users = new Dictionary<string, int>()
            };

            //Add
            bool ok = AppstoreApi.AddCommentToAppId(c, a.id);

            //Return OK
            Program.QuickWriteJsonToDoc(e, new Dictionary<string, bool>
            {
                {"ok", ok }
            });
        }
    }
}
