using System;
using System.Collections.Generic;

namespace HmLib
{
    public class Request
    {

        public Request()
        {
            Headers = new Dictionary<string, string>();
            Parameters = new List<object>();
        }

        internal IDictionary<string, string> Headers { get; private set; }

        public string Method { get; set; }

        public ICollection<object> Parameters { get; set; }

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
            var valueBytes = System.Text.Encoding.ASCII.GetBytes(value);
            var valueEncoded = Convert.ToBase64String(valueBytes);

            var headerValue = string.Concat("Basic ", valueEncoded);
            SetHeader(headerKey, headerValue);
        }
    }
}
