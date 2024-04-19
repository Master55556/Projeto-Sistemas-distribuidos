using System.Net.Sockets;
using System.Text;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Cliente_Mota
{
    public class Program
    {

        public static void Main()
        {
            connectServer();
        }

        public static void connectServer()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 5500);

            Socket sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            startSender(remoteEP, sender);

        }

        public static void startSender(IPEndPoint remoteEP, Socket sender)
        {
            try
            {
                sender.Connect(remoteEP);
                Console.WriteLine("Socket connected to {0}", sender.RemoteEndPoint.ToString());

                byte[] msg = new byte[1024];
                int bytesRec = sender.Receive(msg);
                string data = Encoding.ASCII.GetString(msg, 0, bytesRec);
                Console.WriteLine(data);

                string clientType = getClientType(sender);


                while (true)
                {
                    Console.Write("Enter message: ");
                    string message = Console.ReadLine();

                    msg = Encoding.ASCII.GetBytes(message);

                    int bytesSent = sender.Send(msg);

                    byte[] bytes = new byte[1024];
                    bytesRec = sender.Receive(bytes);

                    data = null;
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);

                    Console.WriteLine(data);

                    if (data == "404 BYE") break;

                }


                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static string getClientType(Socket sender)
        {
            string message;     

            do
            {
                Console.Write("Introduza o id do cliente (Cl_####): ");
                message = Console.ReadLine()!;
            } while (!Regex.Match(message, @"Cl_\d{4}").Success);       

            byte[] msg = Encoding.ASCII.GetBytes(message);

            int bytesSent = sender.Send(msg);

            byte[] bytes = new byte[1024];
            int bytesRec = sender.Receive(bytes);

            string data = null;
            data += Encoding.ASCII.GetString(bytes, 0, bytesRec);

            Console.WriteLine(data);
            if (data == "Nenhuma tarefa atribuida para este cliente.")
            {
                Console.Write("\nPara Adequirir nova tarefa introduza 's'\n\n");
                message = Console.ReadLine();

                msg = Encoding.ASCII.GetBytes(message);

                 bytesSent = sender.Send(msg);

                bytesRec = sender.Receive(bytes);

                data = null;
                data += Encoding.ASCII.GetString(bytes, 0, bytesRec);

                Console.WriteLine(data);

                bytesRec = sender.Receive(bytes);

                data = null;
                data += Encoding.ASCII.GetString(bytes, 0, bytesRec);

                Console.WriteLine(data);
            }

            return data;

        }
    }
}