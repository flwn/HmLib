using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HmLib
{
    using Binary;

    public class HmRpcClient : IDisposable
    {


        private static readonly byte[] MessageHeader = Encoding.ASCII.GetBytes("Bin");

        private readonly IPEndPoint _endpoint;
        private static readonly Encoding Encoding = Encoding.ASCII;

        private enum PacketType : byte
        {
            BinaryRequest = 0x00,
            BinaryResponse = 0x01,
            BinaryRequestHeader = 0x40,
            BinaryResponseHeader = 0x41,
            ErrorResponse = 0xff,
        }

        private readonly string _host;
        private readonly int _port;

        public class Request
        {
            public Request()
            {
                Headers = new Dictionary<string, string>();
                Parameters = new List<object>();
            }

            internal IDictionary<string, string> Headers { get; private set; }

            public string Method { get; set; }

            public ICollection<object> Parameters { get; private set; }

            internal int HeaderLength { get; private set; }

            public void SetHeader(string key, string value)
            {
                if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException("key");
                if (value == null) throw new ArgumentNullException("value");

                if (Headers.ContainsKey(key))
                {
                    throw new InvalidOperationException("Header already set.");
                }

                var headerLength = 4 + key.Length + 4 + value.Length;

                HeaderLength += headerLength;

                Headers[key] = value;
            }

            public void SetAuthorization(string user, string password)
            {
                const string headerKey = "Authorization";

                var value = string.Concat(user, ":", password);
                //this should be possibly utf8?!
                var valueBytes = Encoding.GetBytes(value);
                var valueEncoded = Convert.ToBase64String(valueBytes);

                var headerValue = string.Concat("Basic ", valueEncoded);
                SetHeader(headerKey, headerValue);
            }
        }

        public class Response
        {
            public bool IsError { get; set; }

            public string Content { get; set; }
        }

        private readonly TcpClient _tcpClient = new TcpClient();

        public HmRpcClient(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public HmRpcClient(IPEndPoint endpoint)
        {
            _endpoint = endpoint;
        }


        public async Task ConnectAsync(string host = null, int? port = null)
        {
            if (_endpoint != null)
            {
                await _tcpClient.ConnectAsync(_endpoint.Address, _endpoint.Port);
            }
            else
            {
                await _tcpClient.ConnectAsync(host ?? _host, port ?? _port);
            }
        }

        public async Task<Response> ExecuteRequest(Request request)
        {
            if (!_tcpClient.Connected)
            {
                throw new InvalidOperationException("Connect first.");
            }

            var networkStream = _tcpClient.GetStream();

            new HmBinaryMessageWriter().WriteRequest(networkStream, request);

            Thread.Sleep(100);

            var reader = new HmBinaryReader(networkStream, true);
            var responseHeader = reader.ReadBytes(3);

            if (!responseHeader.SequenceEqual(MessageHeader))
            {
                Debugger.Break();

            }

            var responseType = (PacketType)reader.ReadByte();

            switch (responseType)
            {
                case PacketType.BinaryResponse:
                case PacketType.BinaryResponseHeader:
                case PacketType.ErrorResponse:

                    //handle response;
                    var responseLength = reader.ReadInt32();


                    var responseContent = new JsonObjectBuilder();
                    ReadResponse(reader, responseContent);
                    

                    var response = new Response
                    {
                        IsError = responseType == PacketType.ErrorResponse,
                        Content = responseContent.ToString()
                    };

                    var bytesRead = reader.BytesRead - 8;//min header bytes

                    if (bytesRead > int.MaxValue || bytesRead != responseLength)
                    {
                        if (Debugger.IsAttached)
                        {
                            Debugger.Break();
                        }
                    }

                    return response;

                case PacketType.BinaryRequest:
                case PacketType.BinaryRequestHeader:
                default:
                    Debugger.Break();
                    throw new ArgumentOutOfRangeException();
            }

        }

        private void ReadResponse(HmBinaryReader reader, JsonObjectBuilder builder)
        {
            var type = (ContentType)reader.ReadInt32();

            switch (type)
            {
                case ContentType.Int:
                    ReadInt(reader, builder);
                    break;
                case ContentType.Boolean:
                    ReadBoolean(reader, builder);
                    break;
                case ContentType.String:
                    ReadString(reader, builder);
                    break;
                case ContentType.Float:
                    ReadFloat(reader, builder);
                    break;
                case ContentType.Base64:
                    ReadBase64(reader, builder);
                    break;
                case ContentType.Array:
                    ReadArray(reader, builder);
                    break;
                case ContentType.Struct:
                    ReadStruct(reader, builder);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ReadStruct(HmBinaryReader reader, JsonObjectBuilder builder)
        {
            var elementCount = reader.ReadInt32();
                        builder.BeginStruct();

            for (; elementCount > 0; elementCount--)
            {
                builder.BeginItem();

                var propertyName = reader.ReadString();
                builder.WritePropertyName(propertyName);
                
                ReadResponse(reader, builder);

                builder.EndItem();
            }

            builder.EndStruct();
        }

        private void ReadArray(HmBinaryReader reader, JsonObjectBuilder builder)
        {
            var itemCount = reader.ReadInt32();
            
            builder.BeginArray();
            for (; itemCount > 0; itemCount--)
            {
                ReadResponse(reader, builder);
            }
            builder.EndArray();
        }

        private void ReadBase64(HmBinaryReader reader, JsonObjectBuilder builder)
        {
            var base64 = reader.ReadString();
            builder.WriteBase64String(base64);
        }

        private void ReadFloat(HmBinaryReader reader, JsonObjectBuilder builder)
        {
            var floatValue = reader.ReadDouble();

            builder.WriteDoubleValue(floatValue);
        }


        private void ReadString(HmBinaryReader reader, JsonObjectBuilder builder)
        {
            var stringValue = reader.ReadString();

            builder.WriteStringValue(stringValue);
        }

        private void ReadInt(HmBinaryReader reader, JsonObjectBuilder builder)
        {
            var number = reader.ReadInt32();
            builder.WriteInt32Value(number);
        }


        private void ReadBoolean(HmBinaryReader reader, JsonObjectBuilder builder)
        {
            var boolean = reader.ReadBoolean();
            builder.WriteBooleanValue(boolean);
        }


        public void Dispose()
        {
            _tcpClient.Close();
        }

    }
}