
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace HmLib.CommandLine
{
    using HmLib.Serialization;
    using Proxy;
    using Proxy.Devices;
    using System.Net.Sockets;
    using Switch = Proxy.Devices.Switch;

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

            var listener = new HmRpcServer(req =>
            {
                return null;
            });

            listener.Start();

            try
            {
                Test().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            //s.Wait();

            Console.ReadLine();
            listener.Dispose();
        }
        public static async Task Test()
        {
            var endpoint = new IPEndPoint(IPAddress.Parse("192.168.63.3"), 2001);
            using (var client = new HmRpcClient(endpoint))
            {
                var stopwatch = new Stopwatch();

                stopwatch.Start();
                await client.ConnectAsync();
                Console.WriteLine("Connect: {0}ms.", stopwatch.Elapsed.TotalMilliseconds);

                var proxy = new GenericProxy(client);

                var req1 = new Request { Method = "setValue", Parameters = { "HEQ0359881:1", "LEVEL", 1d, "FLOAT" } };
                var req2 = new Request { Method = "setValue", Parameters = { "HEQ0359959:1", "LEVEL", 1d, "FLOAT" } };
                var req3 = new Request { Method = "setValue", Parameters = { "IEQ0019224:1", "LEVEL", 0d, "FLOAT" } };
                var req4 = new Request { Method = "setValue", Parameters = { "IEQ0020290:1", "LEVEL", 0d, "FLOAT" } };
                var req5 = new Request { Method = "setValue", Parameters = { "IEQ0020353:1", "LEVEL", 0d, "FLOAT" } };

                var response = await proxy.MultiCall(req1, req2/*, req3, req4, req5*/);

                var req1_ = new Request { Method = "setValue", Parameters = { "HEQ0359881:1", "LEVEL", 0d, "FLOAT" } };
                var req2_ = new Request { Method = "setValue", Parameters = { "HEQ0359959:1", "LEVEL", 0d, "FLOAT" } };
                var req3_ = new Request { Method = "setValue", Parameters = { "IEQ0019224:1", "LEVEL", 1d, "FLOAT" } };
                var req4_ = new Request { Method = "setValue", Parameters = { "IEQ0020290:1", "LEVEL", 1d, "FLOAT" } };
                var req5_ = new Request { Method = "setValue", Parameters = { "IEQ0020353:1", "LEVEL", 1d, "FLOAT" } };

                //var response_ = await proxy.MultiCall(req1_, req2_, req3_, req4_, req5_);
                //return;

                var pong = await client.ExecuteRequest(new Request { Method = "init", Parameters = { "binary://192.168.63.192:6300", "TEST-" + DateTime.Now.ToString("hhmm"), 0 } });
                Console.WriteLine(pong);

                return;

                await GatherInfo(proxy);
                return;

                var defBlind = Devices["blind_door"];
                var blind = (Blind)Activator.CreateInstance(defBlind.Item2, defBlind.Item1, proxy);
                stopwatch.Restart();
                await blind.SetLevel(0.5d);
                Console.WriteLine("Blind.SetLevel: {0}ms.", stopwatch.Elapsed.TotalMilliseconds);

                Console.ReadLine();
                Thread.Sleep(1000);
                await blind.SetLevel(1d);
                return;

                var defDimmer = Devices["dimmer_dining"];
                var dimmer = (Dimmer)Activator.CreateInstance(defDimmer.Item2, defDimmer.Item1, proxy);


                var defSwitch = Devices["switch_hallway"];
                var @switch = (Switch)Activator.CreateInstance(defSwitch.Item2, defSwitch.Item1, proxy);

                await @switch.SetState(true);
                Thread.Sleep(2000);

                await @switch.SetState(false);



                for (double i = 0; i <= 1; i += 0.1)
                {
                    await dimmer.SetLevel(i);
                    Thread.Sleep(500);
                }
                await dimmer.SetLevel(0d);


                //Console.WriteLine(response);
            }

        }

        public static async Task GatherInfo(GenericProxy proxy)
        {
            //File.WriteAllText("serviceMessages.json", await proxy.GetServiceMessages());


            //File.WriteAllText("methods.json", await proxy.ListMethods());
            //File.WriteAllText("interfaces.json", await proxy.ListBidcosInterfaces());
            //File.WriteAllText("devices.json", await proxy.ListDevices("HEQ0356495"));
        }

    }
}
