namespace GrpcServer
{
    public class Service
    {
        public string Task { get; set; }
        public string Desc { get; set; }
        public string State { get; set; }
        public string ClientId { get; set; }

        public Service() { }

        public Service(string task, string desc, string state, string clientId)
        {
            Task = task;
            Desc = desc;
            State = state;
            ClientId = clientId;
        }

        public Service(string[] service)
        {
            Task = service[0];
            Desc = service[1];
            State = service[2];
            ClientId = service[3];
        }

        public override string ToString()
        {
            return $"{Task},{Desc},{State},{ClientId}";
        }
    }
}
