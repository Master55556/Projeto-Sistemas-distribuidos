using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class ServiceClient
    {
        public string ClientId { get; set; }
        public string ServiceId { get; set; }

        public ServiceClient(){}
        public ServiceClient(string ClientId,string ServiceId)
        {
            this.ClientId = ClientId;
            this.ServiceId = ServiceId;
        }

        public ServiceClient(string[] servicesClients)
        {
            this.ClientId = servicesClients[0];
            this.ServiceId = servicesClients[1];
        }

        public string ToString()
        {
            return ClientId + "," + ServiceId;
        }
    }
}
