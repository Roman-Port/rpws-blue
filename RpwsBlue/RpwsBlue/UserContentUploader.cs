using Firebase.Auth;
using Firebase.Database;
using Firebase.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RpwsBlue
{
    public class UserContentUploader
    {
        /* Image processing */

        public static async Task<string> UploadAndProcessImage(byte[] data)
        {
            //Open image
            try
            {
                using (Image<Rgba32> img = Image.Load(data))
                {
                    //Save image
                    return await UploadProcessedImage(img);
                }
            } catch
            {
                throw new Exception("Failed to open image.");
            }
        }

        private static async Task<string> UploadProcessedImage(Image<Rgba32> img)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                //Save to stream
                img.Save(ms, SixLabors.ImageSharp.ImageFormats.Png);

                //Rewind stream
                ms.Position = 0;

                //Upload
                return await UploadFile(ms, "image/png");
            }
        }

        /* Content uploader */

        public static async Task<string> UploadFile(Stream data, string mimeType = "image/png")
        {
            HttpClient client = new HttpClient();
            
            //Generate a random ID
            string id = LibRpws.LibRpwsCore.GenerateRandomString(42);
            while (await CheckIfFileExists(id, client))
                id = LibRpws.LibRpwsCore.GenerateRandomString(42);

            //Upload this content.
            return await UploadFileWithFilename(data, id, mimeType);
        }

        public static async Task<string> UploadFileWithFilename(Stream data, string filename, string mimeType = "image/png")
        {
            HttpClient client = new HttpClient();
            var authProvider = new FirebaseAuthProvider(new FirebaseConfig(Program.config.secure_creds["google_firebase_content_uploads"]["apiKey"]));
            var auth = await authProvider.SignInWithEmailAndPasswordAsync(Program.config.secure_creds["google_firebase_content_uploads"]["user"], Program.config.secure_creds["google_firebase_content_uploads"]["password"]);

            //Upload this content.
            var content = new StreamContent(data);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.FirebaseToken);
            var response = await client.PostAsync(CreateUrl(filename), content);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to upload file to Firebase; Got {response.StatusCode} code as status.");

            //We don't need to read the content. If we land here, it did upload correctly.
            return $"https://user-content.get-rpws.com/{filename}";
        }

        public static async Task<string> UploadFile(byte[] data)
        {
            //Copy bytes into stream
            string output;
            using(MemoryStream ms = new MemoryStream())
            {
                ms.Write(data, 0, data.Length);
                ms.Position = 0;
                output = await UploadFile(ms);
            }
            return output;
        }

        private static string CreateUrl(string id)
        {
            return $"https://firebasestorage.googleapis.com/v0/b/rpws-usercontent.appspot.com/o/{id}";
        }

        private static async Task<bool> CheckIfFileExists(string id, HttpClient c)
        {
            try
            {
                string path = CreateUrl(id);
                var r = await c.GetAsync(path);
                return r.IsSuccessStatusCode;
            } catch
            {
                
                return false;
            }
        }
    }
}
