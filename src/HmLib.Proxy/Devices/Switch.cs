using System.Threading.Tasks;

namespace HmLib.Proxy.Devices
{
    public class Switch
    {
        private readonly GenericProxy _genericProxy;


        public Switch(string address, GenericProxy genericProxy)
        {
            _genericProxy = genericProxy;
            Address = address;
        }

        public string Address { get; }

        public async Task SetState(bool switchOn)
        {
            await _genericProxy.SetValue(Address + ":1", "STATE", switchOn, "BOOL");
        }
    }
}
