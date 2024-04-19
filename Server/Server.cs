using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.IO;
using Microsoft.VisualBasic;


namespace Server
{
    internal class Server
    {

        private static Mutex mutex = new Mutex();

        static void Main()
        {
            createServer();
        }

        public static List<Service> GetServices(string path)
        {
            List<Service> services = new List<Service>();

            List<string[]> servicesCsv = FileHandler.FileReader("Files/Servico_A.csv");

            foreach (string[] s in servicesCsv)
            {
                services.Add(new Service(s));
            }

            return services;
        }

        public static List<ServiceClient> GetServicesClients(string path)
        {
            List<ServiceClient> servicesClients = new List<ServiceClient>();

            List<string[]> servicesClientsCsv = FileHandler.FileReader("Files/Alocacao_Cliente_Servico.csv");

            foreach (string[] sc in servicesClientsCsv)
            {
                servicesClients.Add(new ServiceClient(sc));
            }

            return servicesClients;
        }

        public static void createServer()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 5500);

            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            startListener(localEndPoint, listener);

        }

        public static void startListener(IPEndPoint localEndPoint, Socket listener)
        {
            List<ServiceClient> serviceClients = GetServicesClients("Files/Alocacao_Cliente_Servico.csv");

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                Console.WriteLine("Waiting for a connection...");

                while (true)
                {
                    Socket client = listener.Accept();
                    Console.WriteLine("Client connected");

                    byte[] okMsg = Encoding.ASCII.GetBytes("100 OK");
                    client.Send(okMsg);

                    Thread clientThread = new Thread(() => HandleClient(client, serviceClients));
                    clientThread.Start();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            listener.Close();

        }

        public static void HandleClient(Socket client, List<ServiceClient> serviceClients)
        {
            byte[] bytes = new byte[1024];
            string data;
            byte[] msg;


            data = null;

            int bytesRec = client.Receive(bytes);
            data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
            string ClientId = data;

            string serviceId = serviceClients.FirstOrDefault(serviceClients => serviceClients.ClientId == data)!.ServiceId;


            List<Service> services = GetServices("Files/" + serviceId + ".csv");

            Service task = services.FirstOrDefault(service => service.ClientId == data)!;

            if (task != null)
            {
                msg = Encoding.ASCII.GetBytes(task.ToString());
                client.Send(msg);
            }
            else
            {
                msg = Encoding.ASCII.GetBytes("Nenhuma tarefa atribuida para este cliente.");
                client.Send(msg);
            }


            while (true)
            {
                data = null;

                bytesRec = client.Receive(bytes);
                data += Encoding.ASCII.GetString(bytes, 0, bytesRec);

                Console.WriteLine(data);

                if (data == "QUIT")
                {
                    msg = Encoding.ASCII.GetBytes("404 BYE");
                    client.Send(msg);
                    break;
                }

                if (data == "s")
                {
                    msg = Encoding.ASCII.GetBytes("Atribuindo uma tarefa... ");
                    client.Send(msg);                    
                    Service task2 = GiveTask(services); 
                    if (task2 != null)
                    {
                        services.FirstOrDefault(service => service.Task == task2.Task)!.State = "Em curso";
                        services.FirstOrDefault(service => service.Task == task2.Task)!.ClientId = ClientId;
                        msg = Encoding.ASCII.GetBytes("Tarefa "+task2.Task+" atribuida com sucesso!");
                        FileHandler.FileWriter("Files/" + serviceId + ".csv", services.Select(s => s.ToString()).ToArray());
                    }
                    else
                    {
                        msg = Encoding.ASCII.GetBytes("Erro!");
                    }
                    client.Send(msg);
                }
                else
                {
                    msg = Encoding.ASCII.GetBytes("Server received: " + data);
                    client.Send(msg);
                }

            }
            Console.WriteLine("Client disconnected");

        }


        public static Service GiveTask(List<Service> services)
        {
            Service task = services.FirstOrDefault(s => s.State == "Nao alocado")!;
            if (task != null)
            {
                Console.WriteLine("Tarefa atribuída para o cliente com sucesso.");
            }
            else
            {
                Console.WriteLine("ERRO alocação! o cliente ja tem uma tarefa ou nao pertence ao serviço");
            }
            return task;
        }

    }    

}




