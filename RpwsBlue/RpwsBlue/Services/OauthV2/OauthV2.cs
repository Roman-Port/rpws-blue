using LibRpws;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using LibRpwsDatabase.Entities;
using LibRpws.Users;
using LibRpws.Auth;

namespace RpwsBlue.Services.OauthV2
{
    public class OauthV2
    {
        static List<AuthState> sessions = new List<AuthState>();

        static string GOOGLE_CLIENT_ID
        {
            get
            {
                return Program.config.secure_creds["google_auth"]["client_id"];
            }
        }

        static string GOOGLE_PRIVATE_ID
        {
            get
            {
                return Program.config.secure_creds["google_auth"]["private_id"];
            }
        }

        static readonly Dictionary<E_RPWS_Token_Permissions, PermissionsText> permissionsText = new Dictionary<E_RPWS_Token_Permissions, PermissionsText>
        {
            {
                E_RPWS_Token_Permissions.All,
                new PermissionsText
                {
                    img = "baseline-lock-white-24px",
                    name = "Access to your entire RPWS account."
                }
            },
            {
                E_RPWS_Token_Permissions.Profile,
                new PermissionsText
                {
                    img = "baseline-lock-white-24px",
                    name = "Access to only your e-mail, name, and installed apps."
                }
            },
            {
                E_RPWS_Token_Permissions.Locker,
                new PermissionsText
                {
                    img = "baseline-lock-white-24px",
                    name = "Access to install, like, flag, and manage Pebble apps."
                }
            },
            {
                E_RPWS_Token_Permissions.Developer,
                new PermissionsText
                {
                    img = "baseline-lock-white-24px",
                    name = "Full access to manage Pebble apps you own."
                }
            },
            {
                E_RPWS_Token_Permissions.TimelineReadOnly,
                new PermissionsText
                {
                    img = "baseline-lock-white-24px",
                    name = "Read-only access to your Pebble Timeline."
                }
            },
            {
                E_RPWS_Token_Permissions.TimelineWrite,
                new PermissionsText
                {
                    img = "baseline-lock-white-24px",
                    name = "Write access to your Pebble Timeline."
                }
            }
        };

        class PermissionsText
        {
            public string img;
            public string name;
        }

        /// <summary>
        /// First function that starts everything
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ee"></param>
        public static void BeginFrontend(Microsoft.AspNetCore.Http.HttpContext context, HttpSession ee)
        {
            //We'll redirect them to a URL based on the params.

            //Check to see if all required keys exist
            ee.CheckIfAllExistInGetParams(new string[] { "returnuri", "permissions", "name" });

            //First, generate a class we'll use for the multistep authentication. This is only stored in ram, and will expire after 10 minutes.
            //Generate an ID for this class
            string id = LibRpwsCore.GenerateRandomString(30);
            while (sessions.Find(x => x.googleStateToken == id) != null)
                id = LibRpwsCore.GenerateRandomString(30);
            //Get the return url
            string returnUrl = ee.GET["returnuri"];

            //Now, generate the URL to redirect to.
            string endPath = $"https://{LibRpwsCore.config.public_host}/v1/oauth2/step2/";
            string redir = $"https://accounts.google.com/o/oauth2/v2/auth?scope=email%20profile&access_type=offline&include_granted_scopes=true&state={id}&redirect_uri={System.Web.HttpUtility.UrlEncode(endPath)}&response_type=code&client_id={GOOGLE_CLIENT_ID}";

            //Get the requested permissions.
            List<E_RPWS_Token_Permissions> perms = new List<E_RPWS_Token_Permissions>();

            //Split the permissions offered in the url by dashes and parse them into permissions
            string[] permsSplit = ee.GET["permissions"].Split('-');
            foreach (string s in permsSplit)
            {
                int ss = int.Parse(s);
                if (ss > 5)
                    throw new Exception("Invalid permission.");
                perms.Add((E_RPWS_Token_Permissions)ss);
            }

            //Create object
            AuthState state = new AuthState
            {
                returnUrl = returnUrl,
                creationTime = DateTime.UtcNow,
                googleStateToken = id,
                googleRedir = endPath,
                initial_args = ee.GET
            };

            //Add to sessions
            sessions.Add(state);

            //If this is not on a whitelist of known signin urls, show the consent screen.
            if (ee.GET["returnuri"] == "https://blue.api.get-rpws.com/getgoing/step2/?branch=official")
            {
                //Known good. Just go
                context.Response.Headers.Add("Location", redir);
                Program.QuickWriteToDoc(context, $"You should've been redirected to {redir}.", "text/html", 302);
            } else
            {
                //Serve the page. First, generate permissions html.
                string permsString = "";
                foreach (var perm in perms)
                    permsString += $"<div class=\"permission\"> <div class=\"img\"><img src=\"https://romanport.com/static/icons/{permissionsText[perm].img}.svg\" /></div> <div class=\"text\">{permissionsText[perm].name}</div> </div>";

                string t = TemplateManager.GetTemplate("Services/OauthV2/PermissionsTemplate.html", new string[] { "%PERMS%", "%SIGNIN%", "%NAME%" }, new string[] { permsString, redir, System.Web.HttpUtility.HtmlEncode(ee.GET["name"]) });

                Program.QuickWriteToDoc(context, t);
            }


        }

        class AuthState
        {

            public List<E_RPWS_Token_Permissions> permissions; //Permissions
            public string returnUrl; //The url to return to
            public DateTime creationTime; //The time this was created.
            public string googleStateToken; //Token sent in the state to Google when starting the grant
            public string googleRedir; //The redirect uri sent to Google
            public Dictionary<string, string> initial_args = new Dictionary<string, string>(); //Args sent to the original frontend page via query.

            //Step 2

            public E_RPWS_User user; //The user requested
            public string step3Token; //Token used by step 3.
        }

        public static void Step2(Microsoft.AspNetCore.Http.HttpContext context, HttpSession ee)
        {
            //Google returns to this after the user completes the oauth.

            //First, get the state to find our session.
            AuthState session = sessions.Find(x => x.googleStateToken == ee.GET["state"]);
            if (session == null)
                throw new Exception("Failed to find the session. It might've expired. Please go back and try again.");
            sessions.Remove(session);

            //Create a request to Google to validate this token.
            HttpClient client = new HttpClient();
            var values = new Dictionary<string, string>
            {
                { "code", ee.GET["code"] },
                { "client_id", GOOGLE_CLIENT_ID },
                { "client_secret", GOOGLE_PRIVATE_ID },
                { "redirect_uri", session.googleRedir },
                { "grant_type", "authorization_code" }
            };
            FormUrlEncodedContent postContent = new FormUrlEncodedContent(values);
            HttpResponseMessage response = client.PostAsync("https://www.googleapis.com/oauth2/v4/token", postContent).GetAwaiter().GetResult();
            GoogleReplyData googleReply = JsonConvert.DeserializeObject<GoogleReplyData>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            if (googleReply.error != null)
                throw new Exception($"Error while authenticating with Google: {googleReply.error}.");

            //We'll now use the ID token sent to us by Google to get some data about the user. It is base 64 encoded.
            //Usually, we would validate it. We don't need to do so because it came directly from Google.
            //Be lazy and just get the payload.
            string idTokenPayload = googleReply.id_token.Split('.')[1];
            //Base 64 strings are always multiples of four, so = must be appended to make this fit this.
            while (idTokenPayload.Length % 4 != 0)
                idTokenPayload += "=";
            //Convert this to standard json.
            GoogleIdToken googleId = JsonConvert.DeserializeObject<GoogleIdToken>(Encoding.UTF8.GetString(Convert.FromBase64String(idTokenPayload)));

            //Validate to make sure all values are here.
            if (googleId.email == null)
                throw new Exception("Google did not offer an E-Mail address. Please contact support at https://get-rpws.com/support");
            if (googleId.name == null)
                throw new Exception("Google did not offer a name. Please contact support at https://get-rpws.com/support");
            if (googleId.sub == null)
                throw new Exception("Google did not offer a Google user ID. Please contact support at https://get-rpws.com/support");

            //Now, we'll create a (or access an existing) user
            E_RPWS_User user = LibRpwsUsers.GetUser(googleId.email, googleId.sub, googleId.name);
            if (user == null)
            {
                //Create a account.
                user = LibRpwsUsers.CreateNewUser(googleId.email, googleId.sub, googleId.name);
            }

            //We're going to update the user information while we're here.
            user.name = googleId.name;
            LibRpwsUsers.UpdateUser(user);

            //We'll wait until the next step to create a token.
            session.user = user;

            //Now, we'll redirect to the final oauth endpoint. The end server will request us for information.
            //To do this, generate a one-time token to be used by the end server.
            string step3token = LibRpwsCore.GenerateRandomString(64);
            while (sessions.Find(x => x.step3Token == step3token) != null)
                step3token = LibRpwsCore.GenerateRandomString(64);

            //Save and redirect back.
            session.step3Token = step3token;
            sessions.Add(session);

            string endpoint = $"https://{LibRpwsCore.config.public_host}/v1/oauth2/step3/?grant={step3token}";

            char sepChar = '?';
            if (session.returnUrl.Contains('?'))
                sepChar = '&';
            string redir = $"{session.returnUrl}{sepChar}endpoint={System.Web.HttpUtility.UrlEncode(endpoint)}&grant_token={step3token}&environment={Program.config.environment.ToString()}";
            context.Response.Headers.Add("Location", redir);
            Program.QuickWriteToDoc(context, $"You should've been redirected to {redir}.", "text/html", 302);
        }

        class GoogleReplyData
        {
            public string access_token;
            public string id_token;
            public string error;
            
        }

        //https://developers.google.com/identity/protocols/OpenIDConnect
        class GoogleIdToken
        {
            public string iss;
            public string at_hash;
            public bool email_verified;
            public string sub; //uuid
            public string azp;
            public string email;
            public string profile;
            public string picture; //Profile picture
            public string name; //Name
            public string aud;
            public string iat;
            public string exp;
            public string nonce;
            public string hd;
        }

        /// <summary>
        /// Finish the grant internally.
        /// </summary>
        public static RpwsFinishOauth InternalFinishGrant(string grant_token)
        {
            //This is the final step, requested by our end user's service server.
            //Find the session based on the grant code.
            AuthState session = sessions.Find(x => x.step3Token == grant_token);
            if (session == null)
            {
                return new RpwsFinishOauth
                {
                    ok = false,
                    message = "Session grant is invalid. Please try again.",
                    initial_args = session.initial_args
                };
            }
            //Generate a access token
            string token = LibRpwsAuth.CreateModernAccessToken(session.user, session.permissions);
            //Create a reply and write it.
            RpwsFinishOauth reply = new RpwsFinishOauth
            {
                ok = true,
                access_token = token,
                initial_args = session.initial_args
            };
            
            //Destroy this session.
            sessions.Remove(session);

            return reply;
        }

        public static void FinishGrant(Microsoft.AspNetCore.Http.HttpContext context, HttpSession ee)
        {
            Program.QuickWriteJsonToDoc(context, InternalFinishGrant(ee.GET["grant"]));
        }

        /// <summary>
        /// Final message sent to the end server.
        /// </summary>
        public class RpwsFinishOauth
        {
            public bool ok;
            public Dictionary<string, string> initial_args = new Dictionary<string, string>();

            //If not ok
            public string message;

            //If okay
            public string access_token;
        }
    }
}
