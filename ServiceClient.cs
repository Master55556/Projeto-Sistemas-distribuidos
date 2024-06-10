namespace GrpcServer
{
    public class ServiceClient
    {
        public string ClientId { get; set; }
        public string ServiceId { get; set; }

        public ServiceClient() { }

        public ServiceClient(string clientId, string serviceId)
        {
            ClientId = clientId;
            ServiceId = serviceId;
        }

        public ServiceClient(string[] servicesClients)
        {
            ClientId = servicesClients[0];
            ServiceId = servicesClients[1];
        }

        public override string ToString()
        {
            return $"{ClientId},{ServiceId}";
        }
    }
}
