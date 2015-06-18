using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HmLib.Serialization;

namespace HmLib
{
    public class ErrorResponse : Response
    {
        public ErrorResponse()
        {
            Type = MessageType.Error;
        }


        public override string ToString()
        {
            return string.Concat("Error: ", Content);
        }
    }
}
