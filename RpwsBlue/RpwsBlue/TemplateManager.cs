using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RpwsBlue
{
    public static class TemplateManager
    {
        public static string GetTemplate(string pathname, string[] keys, string[] values)
        {
            if (keys.Length != values.Length)
                throw new Exception("Failed to load template. Keys and values length do not match.");

            if(pathname.StartsWith("Handlers") || pathname.StartsWith("Entities") || pathname.StartsWith("Services") || pathname.StartsWith("rpws"))
            {
                //This is an old relative URL.
                pathname = LibRpws.LibRpwsCore.config.contentDir + pathname;
            }

            string template = File.ReadAllText(pathname);
            for(int i = 0; i<keys.Length; i++)
            {
                template = template.Replace(keys[i], values[i]);
            }
            return template;
        }

        public static string GetRpwsDevTemplate(string pathname, string[] keys, string[] values)
        {
            string template = File.ReadAllText(LibRpws.LibRpwsCore.config.contentDir + "rpws-dev.html");
            template = template.Replace("%%CONTENT%%", GetTemplate(pathname, keys, values));
            return template;
        }

        public static string GetRpwsTemplate(string pathname, string[] keys, string[] values)
        {
            string template = File.ReadAllText(LibRpws.LibRpwsCore.config.contentDir + "rpws.html");
            template = template.Replace("%%CONTENT%%", GetTemplate(pathname, keys, values));
            return template;
        }
    }
}
