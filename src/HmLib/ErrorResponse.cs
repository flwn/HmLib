using System;
using System.Collections.Generic;

namespace HmLib
{
    public class ErrorResponse : Response
    {
        private Tuple<int, string> _values = new Tuple<int, string>(default(int), null);


        public override bool IsErrorResponse => true;

        public ErrorResponse()
        {
            Type = MessageType.Error;
        }

        public string Message
        {
            get { return _values.Item2; }
            set { SetValues(_values.Item1, value); }
        }

        public int Code
        {
            get { return _values.Item1; }
            set { SetValues(value, _values.Item2); }
        }

        public override object Content
        {
            get { return base.Content; }
            set
            {
                if (value == null)
                {
                    SetValues(default(int), null);
                }
                else
                {
                    var dict = value as IDictionary<string, object>;
                    if (dict == null)
                    {
                        throw new ArgumentException("Only a IDictionary<string, object> content is supported.");
                    }

                    object codeVal;
                    var code = dict.TryGetValue("faultCode", out codeVal) && codeVal is int
                        ? (int)codeVal : default(int);

                    object stringVal;
                    dict.TryGetValue("faultString", out stringVal);
                    var message = stringVal as string;

                    SetValues(code, message);
                }
            }
        }

        private void SetValues(int code, string message)
        {
            _values = Tuple.Create(code, message);
            var newValue = new Dictionary<string, object>
            {
                { "faultCode", Code },
                { "faultString", Message }
            };

            base.Content = newValue;
        }

        public override string ToString()
        {
            return string.Format("Error {0}: {1}", Code, Message);
        }
    }
}
