
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
            var testRequest = new Request()
            {
                Method = "system.listMethods",
                Parameters = { "Bla" }
            };
            testRequest.SetAuthorization("wiki", "pedia");

            var bufferStream = new MemoryStream();
            new HmBinaryMessageWriter().WriteRequest(bufferStream, testRequest);
            var buffer = bufferStream.ToArray();
            var testoutput = new StringBuilder((int)buffer.Length * 2);

            foreach (var byte1 in buffer)
            {
                testoutput.AppendFormat("{0:X2}", byte1);
            }
            var expected =
                "42696E400000002F000000010000000D417574686F72697A6174696F6E0000001642617369632064326C72615470775A57527059513D3D000000250000001273797374656D2E6C6973744D6574686F6473000000010000000300000003426C61";

            var success = expected == testoutput.ToString();

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
