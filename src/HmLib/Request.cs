using System;
using System.Collections.Generic;
using System.Linq;

namespace HmLib
{
    using Abstractions;
    using Serialization;

    public class Request : Message, IRequestMessage
    {
        public Request() : base(MessageType.Request)
        {
            Content = new Dictionary<string, object>()
            {
                {"method", null },
                {"parameters", new List<object>() }
            };
        }

        public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();

        public string Method
        {
            get { return (string)((Dictionary<string, object>)Content)?["method"]; }
            set { ((Dictionary<string, object>)Content)["method"] = value; }
        }

        public ICollection<object> Parameters => (List<object>)((Dictionary<string, object>)Content)?["parameters"];


        public void SetHeader(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (Headers.ContainsKey(key))
            {
                throw new InvalidOperationException("Header already set.");
            }

            var headerLength = 4 + key.Length + 4 + value.Length;

            Headers[key] = value;
        }

        public void SetAuthorization(string user, string password)
        {
            const string headerKey = "Authorization";

            var value = string.Concat(user, ":", password);
            //this should be possibly utf8?!
            var valueBytes = System.Text.Encoding.ASCII.GetBytes(value);
            var valueEncoded = Convert.ToBase64String(valueBytes);

            var headerValue = string.Concat("Basic ", valueEncoded);
            SetHeader(headerKey, headerValue);
        }

        public override string ToString() => $"Request Method={Method}. Parameters (Count={Parameters.Count}): {string.Join(", ", Parameters.Select(x => (x ?? string.Empty).ToString()))}.";

        public IMessageReader GetMessageReader() => new MessageReader(this);
    }
}
