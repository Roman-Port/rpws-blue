using LibRpwsDatabase;
using LibRpwsDatabase.Entities;
using MailjetNet;
using MailjetNet.Entities;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using LiteDB;

namespace LibRpws
{
    public static class LibRpwsCore
    {
        public static bool isConnected = false;

        public static Random rand = new Random();

        public static LibRpwsConfigFile config;

        public static Stream loggingStream;

        public static LiteDatabase lite_database;

        public static void Init(string configPathname)
        {
            //Load config.
            config = JsonConvert.DeserializeObject<LibRpwsConfigFile>(File.ReadAllText(configPathname));
            loggingStream = File.Open(config.loggingFile, System.IO.FileMode.OpenOrCreate);
            loggingStream.Position = loggingStream.Length;
            Log("Loaded config from "+configPathname,null, RpwsLogLevel.Standard);
            //Load database.
            lite_database = new LiteDatabase(config.database_file);
            Console.Title = config.server_name;
        }

        /// <summary>
        /// Send myself a critical alert.
        /// </summary>
        /// <param name="message"></param>
        public static void SendCriticalAlert(string message)
        {
            SendUserEmail(new E_RPWS_User
            {
                email = "rvporterfield@gmail.com",
                name = "Critical Alert"
            }, "CRITIAL RPWS ALERT!!", message, message);
        }

        public static T GetObjectHttp<T>(string endpoint, string token = null, Dictionary<string, string> headers = null)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
                request.Headers.Clear();
                request.UserAgent = config.server_name+" @ rvporterfield@gmail.com";
                request.Headers.Add("Accept", "*/*");
                if (token != null)
                    request.Headers.Add("Authorization", "Bearer " + token);
                if(headers != null)
                {
                    foreach (var h in headers)
                        request.Headers.Add(h.Key, h.Value);
                }
                
                string reply;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    reply = reader.ReadToEnd();
                }
                //Deserialize
                return JsonConvert.DeserializeObject<T>(reply);
            } catch (WebException wex)
            {
                string reply;
                using (Stream s = wex.Response.GetResponseStream())
                using (StreamReader reader = new StreamReader(s))
                {
                    reply = reader.ReadToEnd();
                }
                Console.WriteLine(reply);
                throw new Exception("Remote server returned an error.");
            } 
        }

        public static bool SendUserEmail(E_RPWS_User user, string subject, string text, string html)
        {
            MailjetClient client = new MailjetClient(config.secure_creds["mailjet_token"]["token"]);
            MailjetRecipient sender = new MailjetRecipient("RPWS", "noreply@get-rpws.com");
            MailjetRecipient recipient = new MailjetRecipient(user.name, user.email);
            MailjetEmail email = new MailjetEmail(sender, new MailjetRecipient[] { recipient }, subject, text, html);
            return client.SendEmail(email);
        }

        public static bool SendUserEmail(string emailAddress, string subject, string text, string html)
        {
            MailjetClient client = new MailjetClient(config.secure_creds["mailjet_token"]["token"]);
            MailjetRecipient sender = new MailjetRecipient("RPWS", "noreply@get-rpws.com");
            MailjetRecipient recipient = new MailjetRecipient("RPWS User", emailAddress);
            MailjetEmail email = new MailjetEmail(sender, new MailjetRecipient[] { recipient }, subject, text, html);
            return client.SendEmail(email);
        }

        public static bool SendMyselfEmail(string subject, string text, string html)
        {
            return SendUserEmail("rvporterfield@gmail.com", subject, text, html);
        }

        public static void Redirect(Microsoft.AspNetCore.Http.HttpContext e, string location)
        {
            e.Response.Headers.Add("Location", location);
            var response = e.Response;
            response.StatusCode = 302;
            response.ContentType = "text/html";
            string html = "redirecting you to "+location;
            var data = Encoding.UTF8.GetBytes(html);
            response.ContentLength = data.Length;
            response.Body.Write(data, 0, data.Length);
        }

        public static string GenerateRandomString(int length)
        {
            string output = "";
            char[] chars = "qwertyuiopasdfghjklzxcvbnm1234567890QWERTYUIOPASDFGHJKLZXCVBNM".ToCharArray();
            for(int i = 0; i<length; i++)
            {
                output += chars[rand.Next(0, chars.Length)];
            }
            return output;
        }

        public static string GenerateRandomHexString(int length, Random r = null)
        {
            string output = "";
            if (r == null)
                r = rand;
            char[] chars = "1234567890abcde".ToCharArray();
            for (int i = 0; i < length; i++)
            {
                output += chars[r.Next(0, chars.Length)];
            }
            return output;
        }

        public static string GenerateStringInFormat(string format)
        {
            //&: Hex
            //*: Random
            char[] output = format.ToCharArray();
            for(int i = 0; i<output.Length; i++)
            {
                char ii = output[i];
                if (ii == '&')
                    output[i] = GenerateRandomHexString(1)[0];
                if (ii == '*')
                    output[i] = GenerateRandomString(1)[0];
            }
            return new string(output);
        }

        public static void Log(string message, RpwsLogLevel level = RpwsLogLevel.Standard)
        {
            Log(message, null, level);
        }

        public static void Log(string message, HttpSession session ,RpwsLogLevel level = RpwsLogLevel.Standard)
        {
            switch (level)
            {
                case RpwsLogLevel.Critical:
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Red;
                    break;
                case RpwsLogLevel.High:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.BackgroundColor = ConsoleColor.Black;
                    break;
                case RpwsLogLevel.Standard:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.BackgroundColor = ConsoleColor.Black;
                    break;
                case RpwsLogLevel.Status:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Black;
                    break;
                case RpwsLogLevel.Analytics:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.BackgroundColor = ConsoleColor.Black;
                    break;
            }
            string output;
            string time = "["+DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToLongTimeString() + "] ";
            if(session == null)
            {
                output = ("[" + level.ToString() + "] " + time + message);
            } else
            {
                string userId = "(anonymous)";
                if (session.user != null)
                    userId = session.user.uuid;
                output = ("[" + level.ToString() + "] "+time+"<" + userId + "> " + message);
            }
            Console.WriteLine(output);
            byte[] textData = Encoding.UTF8.GetBytes(output + "\r\n");
            loggingStream.Write(textData, 0, textData.Length);
            loggingStream.Flush();
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
        }
    }

    public enum RpwsLogLevel
    {
        Status,
        Standard,
        High,
        Critical,
        Analytics
    }
}
