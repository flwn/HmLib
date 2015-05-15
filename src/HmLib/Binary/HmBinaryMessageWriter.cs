using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace HmLib.Binary
{
    public class HmBinaryMessageWriter
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


        public HmBinaryMessageWriter()
        {
        }


        public void WriteRequest(Stream outputStream, Request request)
        {
            var writer = new HmBinaryWriter(outputStream);

            writer.Write(PacketHeader);


            if (request.Headers.Count > 0)
            {
                writer.Write((byte)PacketType.BinaryRequestHeader);

                SerializeHeaders(writer, request.Headers);
            }
            else
            {
                writer.Write((byte)PacketType.BinaryRequest);
            }

            SerializeContent(writer, request.Method, request.Parameters);
        }
        public Response ReadResponse(Stream inputStream)
        {
            var reader = new HmBinaryReader(inputStream);

            var responseHeader = reader.ReadBytes(3);

            if (!responseHeader.SequenceEqual(PacketHeader))
            {
                throw new InvalidOperationException("Packet Header not recognized.");
            }

            var responseType = (PacketType)reader.ReadByte();

            switch (responseType)
            {
                case PacketType.BinaryResponse:
                case PacketType.BinaryResponseHeader:
                case PacketType.ErrorResponse:
                    
                    var responseLength = reader.ReadInt32();
                    var packageHeaderSize = reader.BytesRead;

                    var responseContent = _objectBuilderFactory();
                    ReadResponse(reader, responseContent);
                    
                    var bytesRead = reader.BytesRead - packageHeaderSize;

                    if (reader.BytesRead > int.MaxValue)
                    {
                        throw new InvalidOperationException("The response message is too large to handle.");
                    }

                    if (bytesRead != responseLength)
                    {
                        throw new InvalidOperationException("The response is incomplete or corrupted.");
                    }

                    var response = new Response
                    {
                        IsError = responseType == PacketType.ErrorResponse,
                        Content = responseContent.ToString()
                    };

                    return response;

                case PacketType.BinaryRequest:
                case PacketType.BinaryRequestHeader:
                default:
                    Debugger.Break();
                    throw new ArgumentOutOfRangeException();
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




        private void ReadResponse(HmBinaryReader reader, IObjectBuilder builder)
        {
            var type = (ContentType)reader.ReadInt32();

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

        private void ReadStruct(HmBinaryReader reader, IObjectBuilder builder)
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

        private void ReadArray(HmBinaryReader reader, IObjectBuilder builder)
        {
            var itemCount = reader.ReadInt32();

            builder.BeginArray();
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