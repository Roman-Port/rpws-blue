using LibRpws;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RpwsBlue.Services.Admin
{
    class AdminConfig
    {
        public static string OnReq(Microsoft.AspNetCore.Http.HttpContext e, HttpSession ee)
        {
            string config = JsonConvert.SerializeObject(Program.config, Formatting.Indented);
            string error = "";
            string errorColor = "red";
            if(Program.FindRequestMethod(e) == RequestHttpMethod.post)
            {
                if (e.Request.Form.ContainsKey("Submit"))
                {
                    //Process this.
                    config = e.Request.Form["config"].ToString();
                    bool ok = ProcessConfigFile(config, out error, out LibRpwsConfigFile f);
                    if (ok)
                    {
                        try
                        {
                            //Back up config file
                            string path = Program.config_pathname;
                            File.Move(path, path + ".bak" + DateTime.UtcNow.Ticks);

                            //Save new file
                            string ser = JsonConvert.SerializeObject(f, Formatting.Indented);
                            File.WriteAllText(path, ser);

                            //Update existing file.
                            LibRpwsCore.config = f;
                        }
                        catch (Exception ex)
                        {
                            error = "Error while saving.";
                        }

                        //Write
                        error = "Configuration file updated. The system may need a restart to save this file.";
                        errorColor = "green";
                    }
                }

            }

            //Write a form to edit this.
            string html = $"<form action=\"/admin/?service=config\" method=\"post\" id=\"configform\"><textarea form=\"configform\" name=\"config\" style=\"width:100%; height:75%;\" spellcheck=\"false\">{System.Web.HttpUtility.HtmlEncode(config)}</textarea><p style=\"color:{errorColor}\">{error}</p><input type=\"submit\" value=\"Update\" name=\"Submit\"></form>";

            return html;
        }

        private static bool ProcessConfigFile(string config, out string error, out LibRpwsConfigFile f)
        {
            //Try to process json
            try
            {
                f = JsonConvert.DeserializeObject<LibRpwsConfigFile>(config);
            } catch (Exception ex)
            {
                //Error
                error = "Failed to read JSON; " + ex.Message;
                f = null;
                return false;
            }

            //Validate
            if(f.appstore_database_location == null || f.appstore_frontpage == null || f.contentDir == null || f.database_file == null || f.listen_ip == null || f.loggingFile == null ||  f.public_host == null || f.secure_creds == null || f.server_name == null)
            {
                error = "Required entry is missing or null.";
                f = null;
                return false;
            }

            //Check file paths
            if(!Directory.Exists(f.appstore_database_location))
            {
                error = $"Directory, '{f.appstore_database_location}' doesn't exist.";
                f = null;
                return false;
            }
            if (!File.Exists(f.database_file))
            {
                error = $"File, '{f.database_file}' doesn't exist.";
                f = null;
                return false;
            }
            if (!Directory.Exists(f.contentDir))
            {
                error = $"Directory, '{f.contentDir}' doesn't exist.";
                f = null;
                return false;
            }
            
            //Ok
            error = "";
            return true;
        }
    }
}
