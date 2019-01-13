using System;
using LibRpws2.Api;
using Newtonsoft.Json;

namespace LibRpwsTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Connecting...");
            LibRpws2Core.InitConnection();
            Console.WriteLine("Connected! Running tests...\n");

            Console.WriteLine("Getting token...");
            var token = Accounts.ValidateToken("jiuoq2sBeoxFweeKnqr5LgANOPaN1cNluhJVjcwxA3d7hrRESiKEylrnbCbcPYykfhfz");
            Console.WriteLine(JsonConvert.SerializeObject(token) + "\n");

            Console.WriteLine("DONE");
            Console.ReadLine();
        }
    }
}
