
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HmLib.CommandLine
{
    using HmLib.Binary;

    public class Program
    {
        public void Main(string[] args)
        {
            Test().Wait();

        }
        public static async Task Test()
        {
            var request = new Request()
            {
                Method = "system.listMethods"
                //Method = "system.methodHelp",
                //Parameters = { "system.listMethods" }
            };
            var endpoint = new IPEndPoint(IPAddress.Parse("192.168.63.3"), 2001);
            using (var client = new HmRpcClient(endpoint))
            {
                await client.ConnectAsync();

                var respone = await client.ExecuteRequest(request);
            }
        }
    }
}
