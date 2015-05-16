
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
    using HmLib.Proxy;

    public class Program
    {
        public void Main(string[] args)
        {
            Test().Wait();
            //Console.ReadLine();
        }
        public static async Task Test()
        {
            var endpoint = new IPEndPoint(IPAddress.Parse("192.168.63.3"), 2001);
            using (var client = new HmRpcClient(endpoint))
            {
                await client.ConnectAsync();

                var proxy = new ProxyClient(client);

                var dimmer1 = "HEQ0359881:1";
                File.WriteAllText("dimmer1.json", await proxy.GetParamset(dimmer1, "VALUES"));
                var dimmer2 = "HEQ0359959:1";
                var gang = "HEQ0353261:1";
                var response = await proxy.MethodHelp("listDevices");

                await proxy.SetValue(dimmer2, "LEVEL", 1d, "FLOAT");

                //File.WriteAllText("methods.json", await proxy.ListMethods());
                //File.WriteAllText("interfaces.json", await proxy.ListBidcosInterfaces());
                //File.WriteAllText("devices.json", await proxy.ListDevices("HEQ0356495"));

                Console.WriteLine(response);
            }
        }
    }
}
