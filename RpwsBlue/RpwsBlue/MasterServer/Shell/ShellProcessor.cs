using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.MasterServer.Shell
{
    static class ShellProcessor
    {
        public static byte[] ProcessShellCommand(string cmd)
        {
            Console.WriteLine("Got shell command");
            //Open stream
            ShellStream ss = new ShellStream();
            ss.ChangeColor(NetShellColor.White);
            string[] split = cmd.Split(' ');
            string args = cmd.Substring(split[0].Length);

            try
            {
                switch (split[0])
                {
                    case "ping":
                        ss.ChangeColor(NetShellColor.Green);
                        ss.WriteText(args);
                        break;
                    case "users":
                        Cmd_Users(ref ss, split);
                        break;
                    case "random":
                        byte[] buf = new byte[int.Parse(split[1])];
                        LibRpws.LibRpwsCore.rand.NextBytes(buf);
                        return buf;
                    case "app_id":
                        string app_id = split[1];
                        var app = AppstoreApi.GetAppById(app_id);
                        return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(app));
                    case "app_uuid":
                        app = AppstoreApi.GetAppByUUID(split[1]);
                        return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(app));
                    default:
                        ss.ChangeColor(NetShellColor.Red);
                        ss.WriteText("Unknown command.");
                        break;
                }
            } catch (Exception ex)
            {
                Console.WriteLine("Shell error");
                ss.ChangeColor(NetShellColor.Red);
                ss.WriteText("Error! ");
                ss.ChangeColor(NetShellColor.Yellow);
                ss.WriteText(ex.Message);
                ss.ChangeColor(NetShellColor.Red);
                ss.WriteText(" at ");
                ss.ChangeColor(NetShellColor.Yellow);
                ss.WriteText(ex.StackTrace);

            }

            return ss.ToArray();
        }

        private static void Cmd_Users(ref ShellStream ss, string[] split)
        {
            int offset = int.Parse(split[1]);
            int limit = int.Parse(split[2]);
            //Get users.
            var users = LibRpws.Users.LibRpwsUsers.GetCollection().Find(x => true, offset, limit);
            //Write top
            ss.WriteText($"RPWS User List - Offset={offset} Limit={limit}\n\n");
            ss.WriteTableEntry("E-Mail", 40);
            ss.WriteTableEntry("Name", 20);
            ss.WriteTableEntry("User ID", 25);
            ss.WriteTableEntry("UUID", 50);
            ss.WriteText("\n");
            //Write users
            foreach(var u in users)
            {
                ss.ChangeColor(NetShellColor.Green);
                ss.WriteTableEntry(u.email, 40);
                ss.ChangeColor(NetShellColor.White);
                ss.WriteTableEntry(u.name, 20);
                ss.WriteTableEntry(u.pebbleId, 24);
                ss.WriteTableEntry(u.uuid, 50);
                ss.WriteText("\n");
            }
        }
    }
}
