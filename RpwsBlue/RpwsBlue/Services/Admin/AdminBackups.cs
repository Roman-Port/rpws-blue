using LibRpws;
using LiteDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace RpwsBlue.Services.Admin
{
    class AdminBackups
    {
        private static string latest_download_token;
        public static string latest_download_path;

        public static string OnReq(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            if(e.Request.Query.ContainsKey("action"))
            {
                string action = e.Request.Query["action"];
                if(action == "main_db_backup")
                {
                    //Shut down the LiteDB correctly and save it.
                    string name = e.Request.Form["name"];
                    try
                    {
                        lock (LibRpwsCore.lite_database)
                        {
                            //Shut down
                            Thread.Sleep(500);
                            LibRpwsCore.lite_database.Dispose();
                            LibRpwsCore.lite_database = null;
                            Thread.Sleep(5000);

                            //Copy
                            latest_download_path = Program.config.database_file + ".bak_" + name;
                            File.Copy(Program.config.database_file, latest_download_path); //It's OK to not escape the name because I will be the only one using this.

                            //Generate a download token.
                            latest_download_token = LibRpwsCore.GenerateRandomString(32);

                            //Restart the database.
                            LibRpwsCore.lite_database = new LiteDatabase(Program.config.database_file);

                            //Wait a moment
                            Thread.Sleep(3000);
                        }
                    } catch (Exception ex)
                    {
                        //Error
                        return $"<p style=\"color:red;\">Error!<br><br>{System.Web.HttpUtility.HtmlEncode(ex.Message+ex.StackTrace)}</p>";
                    }

                    //Now, include a download token.
                    return $"<p style=\"color:green;\">Database backed up! You may download it <a href=\"/admin/?service=db_backups&action=download_token&token={latest_download_token}\" target=\"_blank\">here</a>.</p>";
                } else if(action == "download_token")
                {
                    //Validate token.
                    if(latest_download_token == e.Request.Query["token"])
                    {
                        //Start the download.
                        if(File.Exists(latest_download_path))
                        {
                            try
                            {
                                SendLatestFile(e);
                            } catch (Exception ex)
                            {
                                return $"<p style=\"color:red;\">Error!<br><br>{System.Web.HttpUtility.HtmlEncode(ex.Message + ex.StackTrace)}</p>";
                            }
                            return null;
                        } else
                        {
                            //Error
                            return $"<p style=\"color:red;\">Failed to download. This backup file could not be found.</p>";
                        }
                    } else
                    {
                        //Error
                        return $"<p style=\"color:red;\">Failed to download. This is an invalid token.</p>";
                    }
                } else
                {
                    //Error
                    return $"<p style=\"color:red;\">Unknown action.</p>";
                }
            } else
            {
                //Serve form
                return $"<form action=\"/admin/?service=db_backups&action=main_db_backup\" method=\"post\" id=\"configform\"><p style=\"color:red; font-size:20px;\">Database backups will interrupt service for up to a minute.</p>Backup name <input type=\"text\" name=\"name\"><br><input type=\"submit\"></form>";
            }
        }

        private static void SendLatestFile(Microsoft.AspNetCore.Http.HttpContext e)
        {
            using (FileStream fs = new FileStream(latest_download_path, System.IO.FileMode.Open))
            {
                //Zip this
                using (MemoryStream ms = new MemoryStream())
                {
                    using (ZipArchive z = new ZipArchive(ms, ZipArchiveMode.Create, true))
                    {
                        //Add database
                        var entry = z.CreateEntry("database.db");
                        Stream openFile = entry.Open();
                        fs.CopyTo(openFile);
                        openFile.Close();

                        //Add metadata
                        var meta = z.CreateEntry("metadata.json");
                        openFile = meta.Open();
                        GetMetadataStream(fs.Length).CopyTo(openFile);
                        openFile.Close();
                    }

                    //Rewind and copy
                    ms.Position = 0;
                    e.Response.ContentLength = ms.Length;
                    ms.CopyTo(e.Response.Body);
                }
                
            }
        }

        private static MemoryStream GetMetadataStream(long size)
        {
            //Create
            ReturnMetadata m = new ReturnMetadata
            {
                create_time = DateTime.UtcNow,
                server_name = Program.config.server_name,
                size = size
            };

            //Serialize
            string s = JsonConvert.SerializeObject(m);
            byte[] sb = Encoding.UTF8.GetBytes(s);

            //To MemoryStream
            MemoryStream ms = new MemoryStream();
            ms.Write(sb, 0, sb.Length);
            ms.Position = 0;
            return ms;
        }

        class ReturnMetadata
        {
            public DateTime create_time;
            public string server_name;
            public long size;
        }
    }
}
