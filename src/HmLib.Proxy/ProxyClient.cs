using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HmLib.Proxy
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class ProxyClient
    {
        public ProxyClient(HmRpcClient rpcClient)
        {
            RpcClient = rpcClient;
        }

        public HmRpcClient RpcClient { get; set; }

        public async Task<string> GetAllMetadata(string objectId)
        {
            if (string.IsNullOrWhiteSpace(objectId)) throw new ArgumentNullException("objectId");
            return await ExecuteMethod("getAllMetadata", objectId);
        }
        public async Task<string> GetDeviceDescription(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentNullException("address");
            return await ExecuteMethod("getDeviceDescription", address);
        }
        public async Task<string> GetParamset(string address, string paramsetKey)
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentNullException("address");
            if (string.IsNullOrWhiteSpace(paramsetKey)) throw new ArgumentNullException("paramsetKey");
            return await ExecuteMethod("getParamset", address, paramsetKey);
        }
        public async Task SetValue(string address, string key, object value, string type)
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentNullException("address");
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentNullException("type");
            await ExecuteMethod("setValue", address, key, value, type);
        }
        public async Task<string> MethodHelp(string methodName)
        {
            if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException("methodName");
            return await ExecuteMethod("system.methodHelp", methodName);
        }

        public async Task<string> ListDevices(string interfaceId)
        {
            if (string.IsNullOrWhiteSpace(interfaceId)) throw new ArgumentNullException("interfaceId");
            return await ExecuteMethod("listDevices", interfaceId);
        }

        public async Task<string> ListBidcosInterfaces()
        {
            return await ExecuteMethod("listBidcosInterfaces");
        }
        public async Task<string> ListMethods()
        {
            return await ExecuteMethod("system.listMethods");
        }

        protected virtual async Task<string> ExecuteMethod(string method, params object[] parameters)
        {
            var request = new Request { Method = method };
            foreach (var param in parameters)
            {
                request.Parameters.Add(param);
            }
            var response = await RpcClient.ExecuteRequest(request);

            return response.Content;
        }
    }
}
