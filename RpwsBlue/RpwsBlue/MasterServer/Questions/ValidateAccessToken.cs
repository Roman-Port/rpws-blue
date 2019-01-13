using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LibRpws;
using LibRpws2.QuestionArgs;
using LibRpwsDatabase.Entities;
using RpwsServerBridge.Entities;

namespace RpwsBlue.MasterServer.Questions
{
    class ValidateAccessToken
    {
        public static void OnEvent(ValidateAccessTokenArgs data, NetworkPacket p)
        {
            //Get the access token from the database.
            var collection = LibRpwsCore.lite_database.GetCollection<E_RPWS_Token>("tokens");
            E_RPWS_Token[] tokens = collection.Find(x => x.token == data.token).ToArray();

            ValidateAccessTokenReplyArgs reply = new ValidateAccessTokenReplyArgs();

            if(tokens.Length == 1)
            {
                //Valid token. Send it over.
                reply.ok = true;
                reply.token = tokens[0];
            } else
            {
                //No token.
                reply.ok = false;
            }

            //Write
            p.ReplyToQuestion(reply);
        }
    }
}
