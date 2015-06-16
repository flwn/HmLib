using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace HmLib.Binary
{
    using Serialization;

    public class HmBinaryProtocol : IProtocol
    {
        private static readonly byte[] PacketHeader = Encoding.ASCII.GetBytes("Bin");

        private enum PacketType : byte
        {
            BinaryRequest = 0x00,
            BinaryResponse = 0x01,
            BinaryRequestHeader = 0x40,
            BinaryResponseHeader = 0x41,
            ErrorResponse = 0xff,
        }


        private readonly HmSerializer _headerSerializer = new HmSerializer { WriteTypeInfoLevel = HmSerializer.WriteTypesFor.Nothing };
        private readonly HmSerializer _bodySerializer = new HmSerializer { WriteTypeInfoLevel = HmSerializer.WriteTypesFor.ChildElements };
        private readonly Func<IObjectBuilder> _objectBuilderFactory = () => new JsonObjectBuilder();


        public HmBinaryProtocol()
        {
        }
        public HmBinaryProtocol(Func<IObjectBuilder> objectBuilderFactory)
        {
            _objectBuilderFactory = objectBuilderFactory;
        }

        public void WriteRequest(Stream outputStream, Request request)
        {
            var writer = new HmBinaryWriter(outputStream);

            writer.WriteRaw(PacketHeader);


            if (request.Headers.Count > 0)
            {
                writer.WriteRaw((byte)PacketType.BinaryRequestHeader);

                SerializeHeaders(writer, request.Headers);
            }
            else
            {
                writer.WriteRaw((byte)PacketType.BinaryRequest);
            }

            SerializeContent(writer, request.Method, request.Parameters);
        }

        public Request ReadRequest(Stream inputStream)
        {
            var reader = new HmBinaryReader(inputStream);

            var responseHeader = reader.ReadBytes(3);

            if (!responseHeader.SequenceEqual(PacketHeader))
            {
                throw new ProtocolException("Packet Header not recognized.");
            }

            var responseType = (PacketType)reader.ReadByte();



            switch (responseType)
            {
                case PacketType.BinaryRequestHeader:
                    ReadHeaders(reader);
                    goto case PacketType.BinaryRequest;

                case PacketType.BinaryRequest:

                    var responseLength = reader.ReadInt32();
                    var contentOffset = reader.BytesRead;

                    var request = new Request();
                    request.Method = reader.ReadString();

                    var builder = _objectBuilderFactory();// new ObjectBuilder();
                    ReadArray(reader, builder);

                    var objectBuilder = builder as ObjectBuilder;
                    if (objectBuilder != null)
                    {
                        request.Parameters = objectBuilder.CollectionResult;
                    }

                    var bytesRead = reader.BytesRead - contentOffset;

                    if (reader.BytesRead > int.MaxValue)
                    {
                        throw new ProtocolException("The response message is too large to handle.");
                    }

                    if (bytesRead != responseLength)
                    {
                        throw new ProtocolException("The response is incomplete or corrupted.");
                    }

                    return request;

                default:
                    Debugger.Break();
                    throw new ProtocolException("Request type not recognized.");
            }
        }

        public void WriteErrorResponse(Stream outputStream, string errorMessage)
        {
            var writer = new HmBinaryWriter(outputStream);

            writer.WriteRaw(PacketHeader);
            writer.WriteRaw((byte)PacketType.ErrorResponse);

            var response = new Dictionary<string, object> { { "faultCode", -10 }, { "faultString", errorMessage } };
            using (var bufferedWriter = HmBinaryWriter.Buffered(writer))
            {
                _bodySerializer.Serialize(bufferedWriter, response);
                bufferedWriter.Flush();
            }

        }

        public void WriteResponse(Stream outputStream, object response)
        {
            var writer = new HmBinaryWriter(outputStream);

            writer.WriteRaw(PacketHeader);

            writer.WriteRaw((byte)PacketType.BinaryResponse);
            using (var bufferedWriter = HmBinaryWriter.Buffered(writer))
            {
                _bodySerializer.Serialize(bufferedWriter, response);
                bufferedWriter.Flush();
            }


        }

        public Response ReadResponse(Stream inputStream)
        {
            var reader = new HmBinaryReader(inputStream);

            var responseHeader = reader.ReadBytes(3);

            if (!responseHeader.SequenceEqual(PacketHeader))
            {
                throw new ProtocolException("Packet Header not recognized.");
            }

            var responseType = (PacketType)reader.ReadByte();

            switch (responseType)
            {
                case PacketType.BinaryResponseHeader:
                    ReadHeaders(reader);
                    goto case PacketType.BinaryResponse;

                case PacketType.BinaryResponse:
                case PacketType.ErrorResponse:

                    var responseLength = reader.ReadInt32();
                    var contentOffset = reader.BytesRead;

                    var responseContent = _objectBuilderFactory();
                    ReadResponse(reader, responseContent);

                    var bytesRead = reader.BytesRead - contentOffset;

                    if (reader.BytesRead > int.MaxValue)
                    {
                        throw new ProtocolException("The response message is too large to handle.");
                    }

                    if (bytesRead != responseLength)
                    {
                        throw new ProtocolException("The response is incomplete or corrupted.");
                    }

                    var response = new Response
                    {
                        IsError = responseType == PacketType.ErrorResponse,
                        Content = responseContent.ToString()
                    };

                    return response;

                default:
                    Debugger.Break();
                    throw new ProtocolException();
            }
        }



        private void SerializeHeaders(HmBinaryWriter writer, IDictionary<string, string> headerDictionary)
        {
            using (var bufferedWriter = HmBinaryWriter.Buffered(writer))
            {
                _headerSerializer.Serialize(bufferedWriter, headerDictionary);

                bufferedWriter.Flush();
            }
        }

        private void SerializeContent(HmBinaryWriter writer, string methodName, ICollection<object> parameters)
        {
            using (var bufferedWriter = HmBinaryWriter.Buffered(writer))
            {
                _headerSerializer.Serialize(bufferedWriter, methodName);

                _bodySerializer.Serialize(bufferedWriter, parameters);

                bufferedWriter.Flush();
            }
        }

        private void ReadHeaders(HmBinaryReader reader)
        {
            var headerLenght = reader.ReadInt32();
            reader.ReadBytes(headerLenght);
        }



        private void ReadResponse(IHmStreamReader reader, IObjectBuilder builder)
        {
            var type = reader.ReadContentType();

            switch (type)
            {
                case ContentType.Array:
                    ReadArray(reader, builder);
                    break;
                case ContentType.Struct:
                    ReadStruct(reader, builder);
                    break;
                case ContentType.Int:
                    builder.WriteInt32Value(reader.ReadInt32());
                    break;
                case ContentType.Boolean:
                    builder.WriteBooleanValue(reader.ReadBoolean());
                    break;
                case ContentType.String:
                    builder.WriteStringValue(reader.ReadString());
                    break;
                case ContentType.Float:
                    builder.WriteDoubleValue(reader.ReadDouble());
                    break;
                case ContentType.Base64:
                    builder.WriteBase64String(reader.ReadString());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ReadStruct(IHmStreamReader reader, IObjectBuilder builder)
        {
            var elementCount = reader.ReadInt32();
            builder.BeginStruct(elementCount);

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

        private void ReadArray(IHmStreamReader reader, IObjectBuilder builder)
        {
            var itemCount = reader.ReadInt32();

            builder.BeginArray(itemCount);
            for (; itemCount > 0; itemCount--)
            {
                builder.BeginItem();
                ReadResponse(reader, builder);
                builder.EndItem();
            }
            builder.EndArray();
        }
    }
}