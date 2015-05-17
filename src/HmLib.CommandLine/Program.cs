
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HmLib.CommandLine
{
    using Proxy;
    using Proxy.Devices;

    public class Program
    {

        private static readonly IDictionary<string, Tuple<string, Type>> Devices = new Dictionary<string, Tuple<string, Type>>
        {
            {"dimmer_dining", new Tuple<string, Type>("HEQ0359881", typeof(Dimmer)) },
            {"dimmer_living", new Tuple<string, Type>("HEQ0359959", typeof(Dimmer)) },
            {"opsteekstekker", new Tuple<string, Type>("HEQ0353261", typeof(Dimmer)) },
            {"switch_hallway", new Tuple<string, Type>("IEQ0097637", typeof(Switch)) },
            {"blind_dining", new Tuple<string, Type>("IEQ0019224", typeof(Blind)) },
            {"blind_door", new Tuple<string, Type>("IEQ0020290", typeof(Blind)) },
            {"blind_big", new Tuple<string, Type>("IEQ0020353", typeof(Blind)) },
        };
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

                var proxy = new GenericProxy(client);


                var defDimmer = Devices["dimmer_dining"];
                var dimmer = (Dimmer)Activator.CreateInstance(defDimmer.Item2, defDimmer.Item1, proxy);

                var defBlind = Devices["blind_dining"];
                var blind = (Blind)Activator.CreateInstance(defBlind.Item2, defBlind.Item1, proxy);

                var defSwitch = Devices["switch_hallway"];
                var @switch = (Switch)Activator.CreateInstance(defSwitch.Item2, defSwitch.Item1, proxy);

                await @switch.SetState(true);
                Thread.Sleep(2000);

                await @switch.SetState(false);

                await blind.SetLevel(0.8d);
                Thread.Sleep(500);
                await blind.SetLevel(1d);


                for (double i = 0; i <= 1; i += 0.1)
                {
                    await dimmer.SetLevel(i);
                    Thread.Sleep(500);
                }
                await dimmer.SetLevel(0d);
                
                //File.WriteAllText("methods.json", await proxy.ListMethods());
                //File.WriteAllText("interfaces.json", await proxy.ListBidcosInterfaces());
                //File.WriteAllText("devices.json", await proxy.ListDevices("HEQ0356495"));

                //Console.WriteLine(response);
            }

        }
            
    }
}
