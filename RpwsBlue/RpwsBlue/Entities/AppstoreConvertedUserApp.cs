
using LibRpws;
using LibRpws2.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Entities
{
    /// <summary>
    /// This endpoint also sends if this app is installed by the user and votes
    /// </summary>
    public class AppstoreConvertedUserApp : AppstoreConvertedApp
    {
        public bool is_auth;
        public AppstoreConvertedUserApp_User me;

        public AppstoreConvertedUserApp(AppstoreApp a, HttpSession e, string platform) : base(a, platform)
        {
            //Check if the user has installed this on the locker.
            if(e.user != null)
            {
                is_auth = true;
                me = new AppstoreConvertedUserApp_User
                {
                    my_vote = AppstoreVoteApi.GetUserVote(a.id, e.user.uuid),
                    is_installed = Services.Locker.LockerTool.GetIsInstalled(e.user.uuid, a.id)
                };
            } else
            {
                is_auth = false;
            }
        }
    }

    public class AppstoreConvertedUserApp_User
    {
        public int my_vote;
        public bool is_installed;
    }
}
