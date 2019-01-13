using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RpwsBlue.Migrations
{
    public static class UserContentMigration
    {
        public static void MigrateUserContent(string path)
        {
            //Migrate user content to Firebase.
            string[] paths = Directory.GetFiles(path);
            foreach(string s in paths)
            {
                //Get filename
                string filename = s.Substring(path.Length).Trim('/').Trim('\\').Split('.')[0].Trim(' ');
                string ext = s.Substring(path.Length).Trim('/').Trim('\\').Split('.')[1].Trim(' ').ToLower();

                //Get mime
                string mime = "application/octet-stream";
                if (ext == "png")
                    mime = "image/png";
                if (ext == "jpg")
                    mime = "image/jpg";

                //Open stream
                using (FileStream fs = new FileStream(s, FileMode.Open))
                {
                    //Upload
                    UserContentUploader.UploadFileWithFilename(fs, filename, mime).GetAwaiter().GetResult();
                }

                Console.WriteLine($"Uploaded {filename} ({mime})");
            }
        }
    }
}
