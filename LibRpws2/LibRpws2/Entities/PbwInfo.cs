using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibRpws2.Entities
{
    public class PbwInfo
    {
        public Dictionary<string, PbwPlatformInfo> platform_info { get; set; }
        public PbwAppInfo2 appinfo { get; set; }


        /// <summary>
        /// Platforms proven to be valid.
        /// </summary>
        public string[] valid_platforms { get; set; }

        public PbwPlatformInfo TryGetAppMeta()
        {
            return platform_info[valid_platforms[valid_platforms.Length - 1]];
        }

        public PbwPlatformInfo TryGetAppMeta(string prefrence)
        {
            if (platform_info.ContainsKey(prefrence))
                return platform_info[prefrence];
            return platform_info[valid_platforms[valid_platforms.Length - 1]];
        }
    }

    public class PbwPlatformInfo
    {
        public AppMeta2 appmeta { get; set; }
        public PebbleAppManifest manifest { get; set; }
    }
}
