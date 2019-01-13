using LibRpwsDatabase.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using LibRpws2.QuestionArgs;
using RpwsServerBridge.Entities;
using LibRpws2.ApiEntities;

namespace LibRpws2.Api
{
    public static partial class Accounts
    {
        /// <summary>
        /// Stores cached tokens.
        /// </summary>
        private static CacheBlock<E_RPWS_Token> cache = new CacheBlock<E_RPWS_Token>(1000);

        /// <summary>
        /// Obtains token details, but does not download the entire user data.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static E_RPWS_Token ValidateToken(string token)
        {
            //Check if it is cached.
            bool isCached = cache.TryGetItem(token, out E_RPWS_Token cached_token);
            if (isCached)
                return cached_token;
            
            //Create a request
            ValidateAccessTokenArgs request_args = new ValidateAccessTokenArgs
            {
                token = token
            };

            //Ask server
            ValidateAccessTokenReplyArgs reply = LibRpws2Core.client.SendQuestionGetReplyWait<ValidateAccessTokenReplyArgs>(request_args, RpwsNetQuestion.ValidateAccessToken);

            if (reply.ok)
            {
                //Cache token.
                cache.AddItem(reply.token, token);

                return reply.token;
            }
            return null;
        }
    }
}
