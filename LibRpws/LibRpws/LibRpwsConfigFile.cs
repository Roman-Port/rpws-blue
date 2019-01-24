using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpws
{
    public class LibRpwsConfigFile
    {
        public string server_name; //Name of the server displayed on the process and on errors and logs.

        public string loggingFile; //File to log to. May be very large.
        public string contentDir; //Path to the root of the bin folder for fetching templates.

        public string ssl_cert_path; //Path to the SSL cert if listen_legacy_ssl is enabled.

        public string public_host; //Name, for example "blue-dev.api.get-rpws.com"

        public string database_file; //Lite DB file to store user content.

        public string listen_ip; //IP to listen on....duh
        public int listen_port; //Port to listen on...duh
        public bool listen_legacy_ssl; //Enables legacy listening for SSL on port 443.

        public string appstore_database_location; //Path to the appstore DB

        public int environment; //0: Production, 1: Debug

        public Dictionary<string, Dictionary<string, string>> secure_creds; //Creds for outside resources, such as Firebase.

        public Dictionary<string, LibRpwsConfigFile_AppstoreFrontpage> appstore_frontpage; //Appstore, by type

        public Dictionary<string, string> appstore_frontpage_files; //Paths to files with the split version of the above var
    }

    /* Appstore */
    public class LibRpwsConfigFile_AppstoreFrontpage : ICloneable
    {
        public List<LibRpwsConfigFile_AppstoreFrontpage_Section> sections;
        public List<LibRpwsConfigFile_AppstoreFrontpage_Banner> banners;

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class LibRpwsConfigFile_AppstoreFrontpage_Section : ICloneable
    {
        public string title;
        public LibRpwsConfigFile_AppstoreFrontpage_Section_Action[] actions;
        public int typeId;
        public string[] apps;
        public bool showAllButton;

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class LibRpwsConfigFile_AppstoreFrontpage_Section_Action : ICloneable
    {
        public string text;
        public string href;

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class LibRpwsConfigFile_AppstoreFrontpage_Banner : ICloneable
    {
        public string img;
        public string href;

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
