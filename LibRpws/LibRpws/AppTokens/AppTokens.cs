using LibRpwsDatabase.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace LibRpws.AppTokens
{
    public class AppTokens
    {
        public static string GetOrCreateAppToken(E_RPWS_User user, string appId)
        {
            var collection = LibRpwsCore.lite_database.GetCollection<E_RPWS_AppToken>("app_tokens");
            //First, check if it exists.
            var existingTokens = collection.Find(x => x.accountUuid == user.uuid && x.appId == appId).ToArray();
            if(existingTokens.Length == 1)
            {
                //Token already exists.
                return existingTokens[0].token;
            } else
            {
                //Create token.
                string tokenId = "";
                while (tokenId.Length == 0 || collection.Find(x => x.token == tokenId).Count() != 0)
                {
                    tokenId = LibRpwsCore.GenerateRandomString(32);
                }
                //Create the entity.
                E_RPWS_AppToken token = new E_RPWS_AppToken();
                token.token = tokenId;
                token.accountUuid = user.uuid;
                token.appId = appId;
                token.isOriginalPebble = false;
                token.creationDate = DateTime.UtcNow.Ticks;
                
                //Insert the token into the database.
                collection.Insert(token);
                //Return the token string.
                return tokenId;
            }
            
        }

        public static bool ValidateToken(string token, out string appId, out E_RPWS_User user)
        {
            var collection = LibRpwsCore.lite_database.GetCollection<E_RPWS_AppToken>("app_tokens");
            //First, check if it exists.
            var tokens = collection.Find(x => x.token == token).ToArray();
            
            if(tokens.Length == 1)
            {
                var t = tokens[0];
                appId = t.appId;
                user = LibRpws.Users.LibRpwsUsers.GetUserByUuid(t.accountUuid);
                return true;
            } else
            {
                user = null;
                appId = null;
                return false;
            }
        }
    }
}
