// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Serialization
{
    public class JsonUtf8Serializer : ISerializer
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public JsonUtf8Serializer(IOptions<CapOptions> capOptions)
        {
            _jsonSerializerOptions = capOptions.Value.JsonSerializerOptions;
        }

        public Task<TransportMessage> SerializeAsync(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (message.Value == null)
            {
                return Task.FromResult(new TransportMessage(message.Headers, null));
            }
            string msgValue = string.Empty;
            if (message.Value.GetType() == typeof(string))
                msgValue = message.Value.ToString();
            else
                msgValue = SerializeObj(message.Value);
            var jsonBytes = Encoding.UTF8.GetBytes(msgValue);
            return Task.FromResult(new TransportMessage(message.Headers, jsonBytes));
        }

        public Task<Message> DeserializeAsync(TransportMessage transportMessage, Type? valueType)
        {
            if (valueType == null || transportMessage.Body == null || transportMessage.Body.Length == 0)
            {
                return Task.FromResult(new Message(transportMessage.Headers, null));
            }

            var obj = JsonSerializer.Deserialize(transportMessage.Body, valueType, _jsonSerializerOptions);

            return Task.FromResult(new Message(transportMessage.Headers, obj));
        }

        private string SerializeObj(object obj)
        {
            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb))
            using (Newtonsoft.Json.JsonTextWriter writer = new Newtonsoft.Json.JsonTextWriter(sw))
            {
                writer.QuoteChar = '\'';
                Newtonsoft.Json.JsonSerializer ser = new Newtonsoft.Json.JsonSerializer();
                ser.Serialize(writer, obj);
            }
            return sb.ToString();
        }

        public string Serialize(Message message)
        {
            return SerializeObj(message);
        }

        public Message? Deserialize(string json)
        {
            return JsonSerializer.Deserialize<Message>(json, _jsonSerializerOptions);
        }

        public object? Deserialize(object value, Type valueType)
        {
            if (value is JsonElement jsonElement)
            {
                return JsonSerializer.Deserialize(jsonElement, valueType, _jsonSerializerOptions);
            }

            throw new NotSupportedException("Type is not of type JsonElement");
        }

        public bool IsJsonType(object jsonObject)
        {
            return jsonObject is JsonElement;
        }

    }
}