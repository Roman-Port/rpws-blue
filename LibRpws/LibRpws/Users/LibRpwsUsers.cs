using LibRpwsDatabase.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace LibRpws.Users
{
    public static partial class LibRpwsUsers
    {
        public static LiteDB.LiteCollection<E_RPWS_User> GetCollection()
        {
            var collection = LibRpwsCore.lite_database.GetCollection<E_RPWS_User>("accounts");
            return collection;
        }

        public static E_RPWS_User GetUserByUuid(string uuid)
        {
            var collection = GetCollection();
            //Find this user.
            E_RPWS_User[] users = collection.Find(x => x.uuid == uuid).ToArray();
            if(users.Length == 1)
            {
                return users[0];
            } else
            {
                return null;
            }
        }

        public static E_RPWS_User GetUser(string email, string googleId, string name)
        {
            //Grab the collection.
            var collection = GetCollection();
            //Grab them.
            var d = collection.Find(x => x.email == email || x.googleId == googleId).ToArray();
            if(d.Length == 1)
            {
                return d[0];
            } else
            {
                //Unknown.
                return null;
            }
        }

        /// <summary>
        /// Creates a one-time RPWS user. This is used for testing and will expire if you log out. This also generates an access token.
        /// </summary>
        /// <returns>Acount access token</returns>
        public static E_RPWS_User CreateOneTimeUser()
        {
            //Validate enviornment
            if (LibRpwsCore.config.environment != RPWS_Enviornment.Alpha)
                throw new Exception($"Cannot create test user because the server enviornment is not RPWS_Enviornment.{RPWS_Enviornment.Alpha.ToString()}.");
            
            //First, create the user using fake creds
            string cred = LibRpwsCore.GenerateRandomString(8);
            E_RPWS_User u = CreateNewUser(cred, cred, $"One-Time User {cred}");

            return u;

            //Return an access token for this user
            /*return Auth.LibRpwsAuth.CreateModernAccessToken(u, new List<E_RPWS_Token_Permissions> {
                E_RPWS_Token_Permissions.All
            });*/
        }

        public static E_RPWS_User CreateNewUser(string email, string googleId, string name)
        {
            //Grab the collection.
            var collection = GetCollection();
            //Check if this Google ID or email is already used.
            if(collection.Find(x => x.email == email || x.googleId == googleId).Count() != 0)
            {
                throw new Exception("That email or Google ID is already used. Try signing in.");
            }
            
            E_RPWS_User user = new E_RPWS_User();
            user.email = email;
            user.googleId = googleId;
            user.name = name;
            user.legacyPebbleId = "";
            user.uuid = "";
            user.pebbleId = "";

            //Generate some IDs.
            while(user.uuid.Length == 0 || collection.Find(x=> x.legacyPebbleId == user.legacyPebbleId || x.uuid == user.uuid || x.pebbleId == user.pebbleId).Count() != 0)
            {
                user.legacyPebbleId = "rpws_" + LibRpwsCore.GenerateRandomString(64);
                user.uuid = LibRpwsCore.GenerateRandomString(64);
                user.pebbleId = LibRpwsCore.GenerateRandomHexString(24);
            }

            //Set some more values.
            user.registrationDate = DateTime.UtcNow.Ticks;
            user.isPebbleLinked = false;
            user.isAppDev = false;
            user.appDevName = "";
            user.lockerInstalled = new string[] { "55b6c2c3e68f5acdfc00008d", "55b6cd955e0716cd6b0000a1" };
            user.likedApps = new string[0];

            //Insert this into the table.
            collection.Insert(user);

            return user;
        }

        public static void UpdateUser(E_RPWS_User user)
        {
            //Grab the collection.
            var collection = GetCollection();
            //Update
            collection.Update(user);
        }
    }
}
