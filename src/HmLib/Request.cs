using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public void SetHeader(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

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

        public override string ToString()
        {
            return string.Format("Request Method={0}. Parameters (Count={1}): {2}.", Method, Parameters.Count, string.Join(", ", Parameters.Select(x => (x ?? string.Empty).ToString())));
        }
    }
}
