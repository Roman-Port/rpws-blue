using LibRpws;
using LibRpws2.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Services.Appstore.AppServices
{
    public static class AppVotes
    {
        public static void OnRequest(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee, AppstoreApp a, int vote)
        {
            if (ee.user == null)
                throw new RpwsStandardHttpException("This requires authentication.");

            //Update
            var s = AppstoreVoteApi.VoteOnApp(a.id, ee.user.uuid, vote);

            //Create reply.
            AppVotesReply reply = new AppVotesReply
            {
                my_vote = vote,
                total = s.total
            };

            Program.QuickWriteJsonToDoc(e, reply);
        }

        class AppVotesReply
        {
            public int total;
            public int my_vote;
        }
    }
}
