using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;


public class RabbitMqServer
{
    public RabbitMqServer(string severity = "info", string message = "Hello World!")
    {
        // // Criação de conexões com o RabbitMQ, configurada para se conectar ao host "localhost".
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        //// Cria um canal de comunicação dentro da conexão estabelecida. Este canal é usado para enviar e receber mensagens.
        using var channel = connection.CreateModel();
        // Declara uma exchange
        channel.ExchangeDeclare(exchange: "direct_logs", type: ExchangeType.Direct);

        // // Publica a mensagem na exchange "direct_logs" com a chave de roteamento especificada por "severity".
        var body = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish(exchange: "direct_logs",
                             routingKey: severity,
                             basicProperties: null,
                             body: body);
        Console.WriteLine($" [x] Sent '{severity}':'{message}'");

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }
}
