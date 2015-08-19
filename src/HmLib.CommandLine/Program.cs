﻿
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace HmLib.CommandLine
{
    using Proxy;
    using Proxy.Devices;
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
            var server = Server.RpcServer.Create();

            server.Listener.OnClientConnected += clientInfo =>
            {
                Console.Write("Incoming! (Local={0}, Remote=", clientInfo.LocalEndPoint);
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(clientInfo.RemoteEndPoint);
                Console.ResetColor();
                Console.WriteLine(")");
            };

            server.Listener.OnClientDisconnected += clientInfo =>
            {
                Debug.WriteLine("Client {0} disconnected.", clientInfo.RemoteEndPoint);
            };

            server.Start();

            try
            {
                Test().Wait();
            }
            catch (AggregateException aggrEx)
            {
                var ex = aggrEx.InnerException;
                Debug.WriteLine("Error: {0}", (object)ex.ToString());
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }

            server.Dispose();
        }
        public static async Task Test()
        {
            var endpoint = new IPEndPoint(IPAddress.Parse("192.168.63.3"), 2001);

            using (var rpcClient = new TcpClientRequestHandler(endpoint))
            {
                var client = new HmClient(rpcClient);
                var stopwatch = new Stopwatch();

                stopwatch.Start();
                await rpcClient.ConnectAsync();
                Console.WriteLine("Connect: {0}ms.", stopwatch.Elapsed.TotalMilliseconds);

                var proxy = new GenericProxy(client);

                var req1 = new Request { Method = "setValue", Parameters = { "HEQ0359881:1", "LEVEL", 1d, "FLOAT" } };
                var req2 = new Request { Method = "setValue", Parameters = { "HEQ0359959:1", "LEVEL", 1d, "FLOAT" } };
                var req3 = new Request { Method = "setValue", Parameters = { "IEQ0019224:1", "LEVEL", 0d, "FLOAT" } };
                var req4 = new Request { Method = "setValue", Parameters = { "IEQ0020290:1", "LEVEL", 0d, "FLOAT" } };
                var req5 = new Request { Method = "setValue", Parameters = { "IEQ0020353:1", "LEVEL", 0d, "FLOAT" } };

                var closeBlinds = new[] { req1, req2, req3, req4, req5 };

                var req1_ = new Request { Method = "setValue", Parameters = { "HEQ0359881:1", "LEVEL", 0d, "FLOAT" } };
                var req2_ = new Request { Method = "setValue", Parameters = { "HEQ0359959:1", "LEVEL", 0d, "FLOAT" } };
                var req3_ = new Request { Method = "setValue", Parameters = { "IEQ0019224:1", "LEVEL", 1d, "FLOAT" } };
                var req4_ = new Request { Method = "setValue", Parameters = { "IEQ0020290:1", "LEVEL", 1d, "FLOAT" } };
                var req5_ = new Request { Method = "setValue", Parameters = { "IEQ0020353:1", "LEVEL", 1d, "FLOAT" } };

                var openBlinds = new[] { req1_, req2_, req3_, req4_, req5_ };

                var response = await proxy.MultiCall(openBlinds);
                //return;

                var pong = await client.ExecuteRequest(new Request { Method = "init", Parameters = { "binary://192.168.63.192:6300", "TEST-" + DateTime.Now.ToString("hhmm", System.Globalization.CultureInfo.GetCultureInfo("nl-NL")), 0 } });
                Console.WriteLine(pong);

                Console.ReadLine();
                var deInit = await client.ExecuteRequest(new Request { Method = "init", Parameters = { "binary://192.168.63.192:6300", "", 0 } });


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
            await Task.Yield();
            //File.WriteAllText("serviceMessages.json", await proxy.GetServiceMessages());


            //File.WriteAllText("methods.json", await proxy.ListMethods());
            //File.WriteAllText("interfaces.json", await proxy.ListBidcosInterfaces());
            //File.WriteAllText("devices.json", await proxy.ListDevices("HEQ0356495"));
        }

    }
}
