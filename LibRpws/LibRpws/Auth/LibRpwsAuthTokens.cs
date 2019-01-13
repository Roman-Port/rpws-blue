using LibRpws.Users;
using LibRpwsDatabase.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace LibRpws.Auth
{
    public partial class LibRpwsAuth
    {
        public static string CreateAccessToken(E_RPWS_User user)
        {
            var collection = LibRpwsCore.lite_database.GetCollection<E_RPWS_Token>("tokens");
            //First, generate the token ID.
            string tokenId = "";
            while(tokenId.Length==0 || collection.Find(x => x.token == tokenId).Count() !=0)
            {
                tokenId = LibRpwsCore.GenerateRandomString(42);
            }
            //Create the entity.
            E_RPWS_Token token = new E_RPWS_Token();
            token.token = tokenId;
            token.accountUuid = user.uuid;
            token.creationDate = DateTime.UtcNow.Ticks;
            token.isModern = false;

            //Insert the token into the database.
            collection.Insert(token);
            //Return the token string.
            return tokenId;
        }

        public static string CreateModernAccessToken(E_RPWS_User user, List<E_RPWS_Token_Permissions> perms)
        {
            var collection = LibRpwsCore.lite_database.GetCollection<E_RPWS_Token>("tokens");
            //First, generate the token ID.
            string tokenId = "";
            while (tokenId.Length == 0 || collection.Find(x => x.token == tokenId).Count() != 0)
            {
                tokenId = LibRpwsCore.GenerateRandomString(42);
            }
            //Create the entity.
            E_RPWS_Token token = new E_RPWS_Token();
            token.token = tokenId;
            token.accountUuid = user.uuid;
            token.creationDate = DateTime.UtcNow.Ticks;
            token.isModern = true;
            token.permissions = perms;

            //Insert the token into the database.
            collection.Insert(token);
            //Return the token string.
            return tokenId;
        }

        public static E_RPWS_User ValidateAccessToken(string token, HttpSession session = null)
        {
            //Check the database to see if we have this token. 
            var collection = LibRpwsCore.lite_database.GetCollection<E_RPWS_Token>("tokens");
            E_RPWS_Token[] tokens = collection.Find(x => x.token == token).ToArray();
            if(tokens.Length == 1)
            {
                //Get this user.
                return LibRpwsUsers.GetUserByUuid(tokens[0].accountUuid);
            } else
            {
                //Not a valid token.
                return null;
            }
        }
    }
}
