using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Service
    {
        public string Task { get; set; }
        public string Desc { get; set; }
        public string State { get; set; }
        public string ClientId { get; set; }

        public Service() { }

        public Service(string Task, string Desc, string State, string ClientId)
        {
            this.Task = Task;
            this.Desc = Desc;
            this.State = State;
            this.ClientId = ClientId;
        }

        public Service(string[] service)
        {
            Task = service[0];
            Desc = service[1];
            State = service[2];
            ClientId = service[3];
        }

        
        public string ToString() 
        {
            return Task+","+Desc+","+State+","+ClientId;
        }

        
    }
}
