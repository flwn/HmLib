﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HmLib.Proxy.Devices
{
    public class Blind
    {
        private readonly GenericProxy _genericProxy;


        public Blind(string address, GenericProxy genericProxy)
        {
            _genericProxy = genericProxy;
            Address = address;
        }

        public string Address { get; private set; }

        public async Task SetLevel(double level)
        {
            await _genericProxy.SetValue(Address + ":1", "LEVEL", level, "FLOAT");
        }
    }
}
