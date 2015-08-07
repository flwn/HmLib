using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HmLib.Proxy
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class GenericProxy
    {
        public GenericProxy(HmClient rpcClient)
        {
            RpcClient = rpcClient;
        }

        public HmClient RpcClient { get; set; }

        public async Task<IDictionary<string, object>> GetAllMetadata(string objectId)
        {
            if (string.IsNullOrWhiteSpace(objectId)) throw new ArgumentNullException(nameof(objectId));
            return (IDictionary<string, object>)await ExecuteMethod("getAllMetadata", objectId);
        }
        public async Task<IDictionary<string, object>> GetDeviceDescription(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentNullException(nameof(address));
            return (IDictionary<string, object>)await ExecuteMethod("getDeviceDescription", address);
        }
        public async Task<IDictionary<string, object>> GetParamset(string address, string paramsetKey)
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentNullException(nameof(address));
            if (string.IsNullOrWhiteSpace(paramsetKey)) throw new ArgumentNullException(nameof(paramsetKey));
            return (IDictionary<string, object>)await ExecuteMethod("getParamset", address, paramsetKey);
        }

        public async Task<object> GetValue(string address, string key)
        {
            var result = await ExecuteMethod("getValue", address, key);

            return result;
        }

        public async Task SetValue(string address, string key, bool value)
        {
            await SetValue(address, key, value, "BOOL");
        }

        public async Task SetValue(string address, string key, int value)
        {
            await SetValue(address, key, value, "INTEGER");
        }

        public async Task SetValue(string address, string key, double value)
        {
            await SetValue(address, key, value, "FLOAT");
        }
        public async Task SetValue(string address, string key, string value)
        {
            await SetValue(address, key, value, "STRING");
        }

        public async Task SetValue(string address, string key, object value, string type)
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentNullException(nameof(address));
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentNullException(nameof(type));

            type = type.ToUpper();
            if (type == "FLOAT")
            {
                if (!(value is double)) throw new ArgumentException("value must be of type Double", nameof(value));
            }
            else if (type == "BOOL")
            {
                if (!(value is bool)) throw new ArgumentException("value must be of type Boolean", nameof(value));
            }
            else if (type == "STRING")
            {
                if (!(value is string)) throw new ArgumentException("value must be of type String", nameof(value));
            }
            else if (type == "INTEGER" || type == "ENUM")
            {
                if (!(value is int)) throw new ArgumentException("value must be of type Int32", nameof(value));
            }

            await ExecuteMethod("setValue", address, key, value, type);
        }

        public async Task<ICollection<object>> ListDevices(string interfaceId)
        {
            if (string.IsNullOrWhiteSpace(interfaceId)) throw new ArgumentNullException(nameof(interfaceId));
            return (ICollection<object>)await ExecuteMethod("listDevices", interfaceId);
        }

        public async Task<ICollection<object>> ListBidcosInterfaces() => (ICollection<object>)await ExecuteMethod("listBidcosInterfaces");

        public async Task<ICollection<object>> GetServiceMessages() => (ICollection<object>)await ExecuteMethod("getServiceMessages");

        public async Task<ICollection<object>> ListMethods() => (ICollection<object>)await ExecuteMethod("system.listMethods");

        public async Task<string> MethodHelp(string methodName)
        {
            if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(methodName));
            return (string)await ExecuteMethod("system.methodHelp", methodName);
        }

        public async Task<object> MultiCall(params Request[] requests)
        {
            var multiCallParams = new List<object>(
                requests
                    .Select(x => new Dictionary<string, object> {
                        { "methodName", x.Method },
                        { "params", x.Parameters }
                    }));

            return await ExecuteMethod("system.multicall", multiCallParams);
        }

        protected virtual async Task<object> ExecuteMethod(string method, params object[] parameters)
        {
            var request = new Request { Method = method };
            foreach (var param in parameters)
            {
                request.Parameters.Add(param);
            }

            var response = await RpcClient.ExecuteRequest<Request, Response>(request);

            return response.Content;
        }

    }
}
