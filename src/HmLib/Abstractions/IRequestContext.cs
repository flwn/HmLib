using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HmLib.Abstractions
{

    public interface IRequestContext
    {
        /// <summary>
        /// Binary, XmlRpc, Other?
        /// </summary>
        string Protocol { get; }

        IMessageReader Request { get; }
        IMessageBuilder Response { get; }
    }
}
