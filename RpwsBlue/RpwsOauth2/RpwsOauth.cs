using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;

namespace RpwsOauth2
{
    public static class RpwsOauth
    {
        public static string GetUrl(string returnPath)
        {
            return $"https://blue.api.get-rpws.com/v1/oauth2/?returnuri={System.Web.HttpUtility.UrlEncode(returnPath)}";
        }

        public static string FinishOauth(string endpoint)
        {
            //Connect to the endpoint provided and get the access token.
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            string payload;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                payload = reader.ReadToEnd();
            }

            //Decode payload
            RpwsPayload p = JsonConvert.DeserializeObject<RpwsPayload>(payload);

            if (p.ok == false)
                throw new Exception($"Failed to authenticate with RPWS; {p.message}");

            return p.access_token;
        }

        class RpwsPayload
        {
            public bool ok;
            public string message;
            public string access_token;
        }
    }
}
