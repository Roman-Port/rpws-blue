using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace LibRpws2
{
    /// <summary>
    /// Contains common actions.
    /// </summary>
    public static class LibRpws2Tools
    {
        public static MemoryStream DownloadFile(string url)
        {
            MemoryStream ms = new MemoryStream();
            //Download and copy to this stream.
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            {
                stream.CopyTo(ms);
            }
            ms.Position = 0;
            return ms;
        }
    }
}
